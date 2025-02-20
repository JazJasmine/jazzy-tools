#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Jazzy
{
    public class ToggleData
    {
        public readonly static string INITIAL_NAME = "New Toggle";
        string name;
        string sanitizedName;
        VRCExpressionsMenu menu;
        bool defaultValue;
        bool saved;
        string objectCount;
        List<GameObject> objects;

        public ToggleData()
        {
            name = INITIAL_NAME;
            sanitizedName = INITIAL_NAME;
            defaultValue = false;
            saved = true;
            objectCount = "1";
            objects = new List<GameObject>();
        }

        public ToggleData(VRCExpressionParameters.Parameter parameter)
        {
            name = parameter.name.Replace($"{Toggles.TOGGLE_PREFIX}/", "");
            sanitizedName = parameter.name.Replace($"{Toggles.TOGGLE_PREFIX}/", "").Replace("/", "");
            defaultValue = parameter.defaultValue == 1f;
            saved = parameter.saved;
            objectCount = "1";
            objects = new List<GameObject>();
        }

        public string Name { get => name; set { sanitizedName = value.Replace("/", ""); name = value; } }
        public string SanitizedName { get => sanitizedName; }
        public VRCExpressionsMenu Menu { get => menu; set => menu = value; }
        public bool DefaultValue { get => defaultValue; set => defaultValue = value; }
        public bool Saved { get => saved; set => saved = value; }
        public string ObjectCountString { get => objectCount; set => objectCount = value; }
        public int ObjectCount { get => Int16.Parse(objectCount); }
        public List<GameObject> Objects { get => objects; set => objects = value; }
    }

    public class Toggles : ScriptableObject
    {
        static public readonly string TOGGLE_PREFIX = "Jazzy";
        static readonly string SEPARATOR = "--- Jazzy Toggles START ---";
        static readonly string SEPARATOR_END = "--- Jazzy Toggles END ---";

        VRCAvatarDescriptor selectedAvatar;
        AnimatorController fxAnimatorController;
        VRCExpressionParameters vrcParameters;
        VRCExpressionsMenu vrcMenu;

        string assetTogglesPath;
        bool isGathered = false;

        List<ToggleData> toggles = new List<ToggleData>();
        List<string> errorMessages;


        public void SelectAvatar(VRCAvatarDescriptor avatar)
        {
            selectedAvatar = avatar;
            assetTogglesPath = $"Assets/{selectedAvatar.name}/VRC/Toggles";
            GatherAvatarInfo();
        }

        void GatherAvatarInfo()
        {
            var controller = selectedAvatar.baseAnimationLayers[4].animatorController;
            fxAnimatorController = controller != null ? (AnimatorController)controller : null;
            vrcParameters = selectedAvatar.GetComponent<VRCAvatarDescriptor>().expressionParameters;
            vrcMenu = selectedAvatar.GetComponent<VRCAvatarDescriptor>().expressionsMenu;
        }

        public void GatherExistingToggles()
        {
            if (isGathered) return;

            if (errorMessages.Count > 0) return; // Errors exist: Don't gather values

            var parameters = Utils.AllJazzyParameters(vrcParameters.parameters);
            if (parameters.Count <= 0) return; // No parameters existed

            foreach (var parameter in parameters)
            {
                if (parameter.name.StartsWith($"{TOGGLE_PREFIX}/Outfits")) continue;

                var toggle = new ToggleData(parameter);
                var clip = (AnimationClip)AssetDatabase.LoadAssetAtPath($"{assetTogglesPath}/{toggle.SanitizedName}On.anim", typeof(AnimationClip));

                var bindings = AnimationUtility.GetCurveBindings(clip);
                var gameObjects = new List<GameObject>();
                foreach (var binding in bindings)
                {
                    var gameObject = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(g => g.name == binding.path);
                    if (gameObject != null) gameObjects.Add(gameObject);

                }

                toggle.Objects = gameObjects;
                toggle.ObjectCountString = gameObjects.Count.ToString();

                toggles.Add(toggle);
            }

            isGathered = true;
        }

        public void ValidateAvatarInfo()
        {
            errorMessages = new List<string>();

            if (fxAnimatorController == null) errorMessages.Add("No FX controller is assigned to the avatar.");
            if (vrcParameters == null) errorMessages.Add("No expression parameters are assigned to the avatar.");
            if (vrcMenu == null) errorMessages.Add("No expression menus are assigned to the avatar.");
        }

        public void AddToggle()
        {
            toggles.Add(new ToggleData());
        }

        public void ClearToggles()
        {
            if (Utils.AnimatorLayerExists(SEPARATOR, fxAnimatorController.layers, out int seperatorIndex)) fxAnimatorController.RemoveLayer(seperatorIndex);
            if (Utils.AnimatorLayerExists(SEPARATOR_END, fxAnimatorController.layers, out int seperatorEndIndex)) fxAnimatorController.RemoveLayer(seperatorEndIndex);

            foreach (var toggle in toggles)
            {
                var jazzyToggleName = $"{TOGGLE_PREFIX}/{toggle.Name}";

                // Clean up Layer
                if (Utils.AnimatorLayerExists(toggle.Name, fxAnimatorController.layers, out int layerIndex)) fxAnimatorController.RemoveLayer(layerIndex);

                // Clean up Parameter
                if (!Utils.AnimatorParameterExists(jazzyToggleName, fxAnimatorController.parameters, out int parameterIndex)) Debug.Log(parameterIndex); fxAnimatorController.RemoveParameter(parameterIndex);

                AssetDatabase.DeleteAsset($"{assetTogglesPath}/{toggle.SanitizedName}On.anim");
                AssetDatabase.DeleteAsset($"{assetTogglesPath}/{toggle.SanitizedName}Off.anim");
            }

            vrcParameters.parameters = Utils.NoJazzyParameters(vrcParameters.parameters).ToArray();
            toggles = new List<ToggleData>();
        }

        public void CreateAssetFolders()
        {
            Utils.CreateFolderIfNotExist($"Assets", selectedAvatar.name);
            Utils.CreateFolderIfNotExist($"Assets/{selectedAvatar.name}", "VRC");
            Utils.CreateFolderIfNotExist($"Assets/{selectedAvatar.name}/VRC", "Toggles");
        }

        public void CreateAnimationClips()
        {

            foreach (var toggle in toggles)
            {
                AnimationClip onClip = new AnimationClip();
                AnimationClip offClip = new AnimationClip();

                foreach (var gameObject in toggle.Objects)
                {
                    if (gameObject == null) continue;
                    var relativePath = Utils.RelativeGameObjectPath(gameObject);
                    onClip.SetCurve(relativePath, typeof(GameObject), "m_IsActive", new AnimationCurve(new Keyframe(0, 1, 0, 0)));
                    offClip.SetCurve(relativePath, typeof(GameObject), "m_IsActive", new AnimationCurve(new Keyframe(0, 0, 0, 0)));
                }

                AssetDatabase.CreateAsset(onClip, $"{assetTogglesPath}/{toggle.SanitizedName}On.anim");
                AssetDatabase.CreateAsset(offClip, $"{assetTogglesPath}/{toggle.SanitizedName}Off.anim");
            }
        }

        public void ApplyToAnimator()
        {
            if (Utils.AnimatorLayerExists(SEPARATOR, fxAnimatorController.layers, out int seperatorIndex)) fxAnimatorController.RemoveLayer(seperatorIndex);
            if (Utils.AnimatorLayerExists(SEPARATOR_END, fxAnimatorController.layers, out int seperatorEndIndex)) fxAnimatorController.RemoveLayer(seperatorEndIndex);

            fxAnimatorController.AddLayer(SEPARATOR);


            foreach (var toggle in toggles)
            {
                var jazzyToggleName = $"{TOGGLE_PREFIX}/{toggle.Name}";
                if (Utils.AnimatorLayerExists(toggle.Name, fxAnimatorController.layers, out int layerIndex)) fxAnimatorController.RemoveLayer(layerIndex);
                if (!Utils.AnimatorParameterExists(jazzyToggleName, fxAnimatorController.parameters)) fxAnimatorController.AddParameter(jazzyToggleName, AnimatorControllerParameterType.Bool);

                fxAnimatorController.AddLayer(toggle.Name);
                if (fxAnimatorController.layers[fxAnimatorController.layers.Length - 1] == null) return;

                var stateMachine = fxAnimatorController.layers[fxAnimatorController.layers.Length - 1].stateMachine;

                if (stateMachine == null) return;

                var onState = stateMachine.AddState($"{toggle.Name}_On", new Vector3(30, 200, 0));
                var offState = stateMachine.AddState($"{toggle.Name}_Off", new Vector3(30, 300, 0));

                onState.writeDefaultValues = false;
                offState.writeDefaultValues = false;

                onState.motion = (Motion)AssetDatabase.LoadAssetAtPath($"{assetTogglesPath}/{toggle.SanitizedName}On.anim", typeof(Motion));
                offState.motion = (Motion)AssetDatabase.LoadAssetAtPath($"{assetTogglesPath}/{toggle.SanitizedName}Off.anim", typeof(Motion));

                var onToOffTranstion = onState.AddTransition(offState);
                var offToOnTranstion = offState.AddTransition(onState);

                onToOffTranstion.duration = 0;
                onToOffTranstion.exitTime = 0;
                onToOffTranstion.hasExitTime = false;
                onToOffTranstion.AddCondition(AnimatorConditionMode.IfNot, 0, jazzyToggleName);

                offToOnTranstion.duration = 0;
                offToOnTranstion.exitTime = 0;
                offToOnTranstion.hasExitTime = false;
                offToOnTranstion.AddCondition(AnimatorConditionMode.If, 0, jazzyToggleName);

                AnimatorControllerLayer[] layers = fxAnimatorController.layers;
                layers[fxAnimatorController.layers.Length - 1].defaultWeight = 1;
                fxAnimatorController.layers = layers;
            }

            fxAnimatorController.AddLayer(SEPARATOR_END);
            AssetDatabase.SaveAssets();
        }

        public void CreateVrcParameters()
        {
            List<VRCExpressionParameters.Parameter> parameters = new List<VRCExpressionParameters.Parameter>();

            foreach (var exisitingParameters in vrcParameters.parameters)
            {
                parameters.Add(exisitingParameters);
            }

            foreach (var toggle in toggles)
            {
                var vrcParameterName = $"{TOGGLE_PREFIX}/{toggle.Name}";
                if (vrcParameters.FindParameter(vrcParameterName) != null) continue; // Ignore if already exists

                parameters.Add(new VRCExpressionParameters.Parameter
                {
                    name = vrcParameterName,
                    valueType = VRCExpressionParameters.ValueType.Bool,
                    defaultValue = toggle.DefaultValue ? 1 : 0,
                    saved = toggle.Saved,
                    networkSynced = true
                });

            }

            vrcParameters.parameters = parameters.ToArray();
        }

        public void PostProcess()
        {
            EditorUtility.SetDirty(fxAnimatorController);
            EditorUtility.SetDirty(vrcParameters);

            AssetDatabase.Refresh();

            AssetDatabase.SaveAssets();
        }

        public List<string> ErrorMessages
        {
            get => errorMessages;
        }

        public List<ToggleData> JazzyToggles
        {
            get => toggles;
        }

        public bool IsGathered { get => isGathered; set => isGathered = value; }
    }
}

#endif