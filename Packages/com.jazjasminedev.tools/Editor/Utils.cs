#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Jazzy
{
    public class Utils
    {
        public static string RelativeGameObjectPath(GameObject gameObject)
        {
            var transform = gameObject.transform;
            string path = transform.name;

            var pathParts = new List<string>() { transform.name };

            while (transform.parent != null)
            {
                transform = transform.parent;
                pathParts.Add(transform.name);
            }

            // Path parts are from inside to outside, so we need to reverse it.
            pathParts.Reverse();
            // First parent is the object itself -> Not needed for the relative path
            pathParts.RemoveAt(0);

            return String.Join("/", pathParts);
        }

        public static void CreateFolderIfNotExist(string parentFolder, string folder)
        {
            var assetFolderPath = $"{parentFolder}/{folder}";
            if (AssetDatabase.IsValidFolder(assetFolderPath)) return;
            AssetDatabase.CreateFolder(parentFolder, folder);
        }

        public static bool AnimatorLayerExists(string layerName, AnimatorControllerLayer[] layers)
        {
            int dropedIndex;
            return AnimatorLayerExists(layerName, layers, out dropedIndex);
        }

        public static bool AnimatorLayerExists(string layerName, AnimatorControllerLayer[] layers, out int index)
        {
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].name == layerName) { index = i; return true; }
            }

            index = -1;
            return false;
        }

        public static bool AnimatorParameterExists(string parameterName, AnimatorControllerParameter[] parameters)
        {
            return AnimatorParameterExists(parameterName, parameters, out _);
        }

        public static bool AnimatorParameterExists(string parameterName, AnimatorControllerParameter[] parameters, out int index)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name == parameterName) { index = i; return true; }
            }

            index = -1;
            return false;
        }

        public static List<VRCExpressionParameters.Parameter> FilterParameters(string filterFor, VRCExpressionParameters.Parameter[] parameters)
        {
            List<VRCExpressionParameters.Parameter> filtered = new List<VRCExpressionParameters.Parameter>();
            foreach (var parameter in parameters)
            {
                if (parameter.name.Contains(filterFor)) filtered.Add(parameter);
            }

            return filtered;
        }

        public static List<VRCExpressionParameters.Parameter> NoJazzyParameters(VRCExpressionParameters.Parameter[] parameters)
        {
            List<VRCExpressionParameters.Parameter> filtered = new List<VRCExpressionParameters.Parameter>();
            foreach (var parameter in parameters)
            {
                if (!parameter.name.Contains(Toggles.TOGGLE_PREFIX)) filtered.Add(parameter);
            }

            return filtered;
        }

        public static List<VRCExpressionParameters.Parameter> AllJazzyParameters(VRCExpressionParameters.Parameter[] parameters)
        {
            return FilterParameters(Toggles.TOGGLE_PREFIX, parameters);
        }

        public GameObject FindObject(GameObject parent, string name)
        {
            Transform[] trs = parent.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in trs)
            {
                if (t.name == name)
                {
                    return t.gameObject;
                }
            }
            return null;
        }

    }
}
#endif