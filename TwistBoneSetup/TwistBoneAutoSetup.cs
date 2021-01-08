using UnityEngine;
using UnityEditor;
using UnityEngine.Animations;
using System.Collections.Generic;

/// <summary>
/// This script sets up twist bones, defaulted to pandas female base
/// Created by Etheri
/// Version 1.4
/// </summary>

#if UNITY_EDITOR
[CustomEditor(typeof(TwistBoneAutoSetup))]
public class TwistBoneAutoSetupEditor : Editor
{

    public override void OnInspectorGUI()
    {
        TwistBoneAutoSetup setup = (TwistBoneAutoSetup)target;
        DrawDefaultInspector();

        GUIStyle box = new GUIStyle(GUI.skin.box);
        box.fontStyle = FontStyle.Bold;
        box.fontSize += 14;
        string warnings = setup.CheckSetup();
        if(warnings == "")
        {
            GUILayout.Box("Looks good ❤, click setup button below to run twist bone setup");
        }
        else
        {
            GUILayout.Box(warnings, box);
        }

        if (GUILayout.Button("Setup Twist Bones"))
        {
            setup.Setup();
        }
    }
}

[ExecuteInEditMode]
public class TwistBoneAutoSetup : MonoBehaviour
{
    public float ankleTwistBoneWeight = 0.7f;
    public float legTwistBoneWeight = 0.65f;
    public float wristTwistBoneWeight = 0.4f;
    public float buttTwistBoneWeight = 0.3f;
    bool dirty = false;

    float timeAtRun = 0.0f;
    List<RotationConstraint> newConstraints = new List<RotationConstraint>();

    public string CheckSetup()
    {
        int armatures = 0;
        Transform[] transforms = GameObject.FindObjectsOfType(typeof(Transform)) as Transform[];
        foreach (Transform n in transforms)
        {
            if (n.name == "Armature")
                armatures++;
        }

        if (armatures == 0)
        {
            //EditorUtility.DisplayDialog("Error", "Could not find avatars armature, is avatar imported correctly?", "ok");
            return "ERROR: Could not find avatars armature, is avatar imported correctly?";
        }
        else if (armatures > 1)
        {
            //EditorUtility.DisplayDialog("Error", "Multiple armatures found! Please only have one avatar enabled at a time", "ok");
            return "ERROR: Multiple armatures found! Please only have one avatar enabled at a time";
        }
        return "";
    }

    

    public void Setup()
    {
        List<ConstraintStatus> results = new List<ConstraintStatus>();
        newConstraints = new List<RotationConstraint>();

        //Ankle
        results.Add(AddConstraint("Twist_Ankle.L", "Left ankle",
            ankleTwistBoneWeight, Axis.Y));

        results.Add(AddConstraint("Twist_Ankle.R", "Right ankle",
            ankleTwistBoneWeight, Axis.Y));

        //Leg
        results.Add(AddConstraint("Twist_Leg.R", "Right leg",
            legTwistBoneWeight, Axis.Y));

        results.Add(AddConstraint("Twist_Leg.L", "Left leg",
            legTwistBoneWeight, Axis.Y));

        //Wrist
        //Sweet dear panda, thanks for your twist bones.
        results.Add(AddConstraint("Wrist_Twist.R", "Right wrist",
            wristTwistBoneWeight, Axis.Y));

        results.Add(AddConstraint("Wrist_Twist.L", "Left wrist",
            wristTwistBoneWeight, Axis.Y));

        //Butt
        results.Add(AddConstraint("Twist_Butt.L", "Left leg",
            buttTwistBoneWeight, Axis.X));

        results.Add(AddConstraint("Twist_Butt.R", "Right leg",
            buttTwistBoneWeight, Axis.X));

        ConstraintStatus finalResult = ConstraintStatus.Success;
        foreach (ConstraintStatus res in results)
        {
            if (res == ConstraintStatus.Error)
            {
                finalResult = ConstraintStatus.Error;
                break;
            }
            if(res == ConstraintStatus.Warning)
            {
                finalResult = ConstraintStatus.Warning;
                break;
            }
            if(res == ConstraintStatus.CATSUsedSuccess)
            {
                finalResult = ConstraintStatus.CATSUsedSuccess;
            }
        }

        if (finalResult == ConstraintStatus.Warning)
        {
            EditorUtility.DisplayDialog("Warning", "Setup finished! Added (" + newConstraints.Count + ") new constraints. \nWarning: Seems you already have rotation constraints setup on a few places. Check Log for more info.", "ok");

            if (newConstraints.Count == 0)
                EditorUtility.DisplayDialog("Warning", "No new constraints added, they are probably already setup!", "ok");
        }
        else if(finalResult == ConstraintStatus.Error)
        {
            EditorUtility.DisplayDialog("Error", "Setup finished! But encountered an error, check log!", "ok");
        }
        else if(finalResult == ConstraintStatus.CATSUsedSuccess)
        {
            EditorUtility.DisplayDialog("Success", "Successful with '_' instead of '.'. \nPlease avoid using CATS Plugin \"Fix Model\"!", "ok");
        }
        else if(finalResult == ConstraintStatus.Success)
        {
            EditorUtility.DisplayDialog("Success", "Setup finished! All good!", "ok");
        }
        

        dirty = true;
        EditorApplication.update += OnEditorUpdate;
        timeAtRun = Time.realtimeSinceStartup;
    }

    enum ConstraintStatus
    {
        Success,
        CATSUsedSuccess,
        Warning,
        Error
    }

    ConstraintStatus AddConstraint(string boneToConstrain,string sourceBone,float weight,Axis freeze)
    {
        var transform = GameObject.Find(boneToConstrain);
        bool catsUsed = false;
        if(transform == null)
        {
            transform = GameObject.Find(boneToConstrain.Replace(".", "_"));
            catsUsed = true;
        }

        if(transform == null)
        {
            EditorUtility.DisplayDialog("Error Twist bone not found!", "The twist bone: " + boneToConstrain + " Could not be found!","ok");
            return ConstraintStatus.Error;
        }
        var bone = transform.gameObject;
        if (bone == null)
        {
            EditorUtility.DisplayDialog("Error Twist bone not found!", "The twist bone: " + boneToConstrain + " Could not be found!", "ok");
            return ConstraintStatus.Error;
        }

        RotationConstraint rc = bone.AddComponent(typeof(RotationConstraint)) as RotationConstraint;
        if (rc != null)
        {
            rc.weight = weight;
            rc.rotationAxis = freeze;
            var sourceTransform = GameObject.Find(sourceBone).transform;
            var source = new ConstraintSource();
            source.sourceTransform = sourceTransform;
            source.weight = 1;
            rc.AddSource(source);
            rc.locked = false;
            rc.constraintActive = true;
            newConstraints.Add(rc);
            if (catsUsed)
            {
                return ConstraintStatus.Success;
            }
            else
            {
                return ConstraintStatus.CATSUsedSuccess;
            }
        }
        else
        {
            Debug.LogWarning("Rotation constraint probably already setup for: " + boneToConstrain);
            return ConstraintStatus.Warning;
        }
    }

    protected virtual void OnEditorUpdate()
    {
        if ((Time.realtimeSinceStartup - timeAtRun) > 1.0f)
        {
            if (dirty)
            {
                foreach (RotationConstraint r in newConstraints)
                {
                    r.locked = true;
                }
            }
            dirty = false;
            EditorApplication.update -= OnEditorUpdate;
        }
    }
}
#endif