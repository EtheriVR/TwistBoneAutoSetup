using UnityEngine;
using UnityEditor;
using UnityEngine.Animations;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// This script sets up twist bones, defaulted to pandas female base
/// Created by Etheri
/// Version 1.5
/// </summary>

namespace EtheriTools.TwistBones
{
    public class TwistBoneAutoSetupEditor : EditorWindow
    {

        [SerializeField] private float ankleTwistBoneWeight = 0.7f;
        [SerializeField] private float legTwistBoneWeight = 0.65f;
        [SerializeField] private float wristTwistBoneWeight = 0.4f;
        [SerializeField] private float buttTwistBoneWeight = 0.3f;

        bool dirty = false;
        float timeAtRun = 0.0f;
        Transform armature;
        List<RotationConstraint> newConstraints = new List<RotationConstraint>();

        [MenuItem("Tools/Etheri - Twist Bone Setup")]
        static void Init()
        {
            TwistBoneAutoSetupEditor window = (TwistBoneAutoSetupEditor)EditorWindow.GetWindow(typeof(TwistBoneAutoSetupEditor), false, "Twist Bone Setup");
            window.Show();
        }

        private void OnGUI()
        {
            Action ShowSetupButton = () =>
            {
                if (GUILayout.Button("Setup Twist Bones"))
                {
                    Setup();
                }
            };

            GUILayout.Label("Armature: ");
            armature = EditorGUILayout.ObjectField(armature, typeof(Transform), true) as Transform;
            GUIStyle box = new GUIStyle(GUI.skin.box);
            box.fontStyle = FontStyle.Bold;
            box.fontSize += 14;

            if (armature != null)
            {
                if (armature.Find("Hips") != null)
                {
                    GUILayout.Box("Looks good ❤, click setup button below to run twist bone setup");
                    ShowSetupButton.Invoke();
                }
                else
                {
                    GUILayout.Box("Hmm questionable, are you sure this is the armature? click setup button below to run twist bone setup",box);
                   ShowSetupButton.Invoke();
                }
            }
            else
            {
                GUILayout.Box("ERROR: Please drag drop your avatars armature into the armature field!", box);
            }


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
                if (res == ConstraintStatus.Warning)
                {
                    finalResult = ConstraintStatus.Warning;
                    break;
                }
                if (res == ConstraintStatus.CATSUsedSuccess)
                {
                    finalResult = ConstraintStatus.CATSUsedSuccess;
                }
            }

            if (finalResult == ConstraintStatus.Warning)
            {
                EditorUtility.DisplayDialog(
                    "Warning",
                    "Setup finished!" + 
                    "Added (" + newConstraints.Count + ") new constraints." +
                    "\nWarning: Seems you already have rotation constraints setup on a few places. Check Log for more info.", "ok");

                if (newConstraints.Count == 0)
                    EditorUtility.DisplayDialog("Warning", "No new constraints added, they are probably already setup!", "ok");
            }
            else if (finalResult == ConstraintStatus.Error)
            {
                //EditorUtility.DisplayDialog("Error", "Setup finished! But encountered an error, check log!", "ok");
            }
            else if (finalResult == ConstraintStatus.CATSUsedSuccess)
            {
                EditorUtility.DisplayDialog("Success", "Successful with '_' instead of '.'. \nPlease avoid using CATS Plugin \"Fix Model/Export\"!", "ok");
            }
            else if (finalResult == ConstraintStatus.Success)
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

        private Transform GetChildByName(Transform transform, string name)
        {
            return transform.GetComponentsInChildren<Transform>().Where(x => x.name == name).FirstOrDefault();
        }


        ConstraintStatus AddConstraint(string boneToConstrain, string sourceBone, float weight, Axis freeze)
        {
            var transform = GetChildByName(armature,boneToConstrain);
            bool catsUsed = false;
            if (transform == null)
            {
                transform = armature.Find(boneToConstrain.Replace(".", "_"));
                catsUsed = true;
            }

            if (transform == null)
            {
                Debug.LogWarning("Error Twist bone not found! The twist bone: " + boneToConstrain + " Could not be found! Skipping.");
                return ConstraintStatus.Error;
            }
            var bone = transform.gameObject;
            if (bone == null)
            {
                Debug.LogWarning("Error Twist bone not found! The twist bone: " + boneToConstrain + " Could not be found! Skipping.");
                return ConstraintStatus.Error;
            }

            RotationConstraint constraint = bone.AddComponent(typeof(RotationConstraint)) as RotationConstraint;
            if (constraint != null)
            {
                constraint.weight = weight;
                constraint.rotationAxis = freeze;

                var sourceTransform = GetChildByName(armature, sourceBone);
                var source = new ConstraintSource();

                source.sourceTransform = sourceTransform;
                source.weight = 1;
                
                constraint.AddSource(source);
                constraint.locked = false;
                constraint.constraintActive = true;
                
                newConstraints.Add(constraint);
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
        
        //Little hack to get around that locking transforms has to happen a bit after we add the component otherwise rotation wont update.
        //And we dont have coroutines in editor...
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
}
