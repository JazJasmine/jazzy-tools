#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace Jazzy
{
    public class OutfitData
    {
        public readonly static string INITIAL_NAME = "New Outfit";
        string name;
        string uuid;
        readonly Dictionary<string, bool> parameterChecks = new Dictionary<string, bool>();

        public OutfitData(string initialName, List<VRCExpressionParameters.Parameter> parameters)
        {
            name = initialName;
            uuid = GUID.Generate().ToString();

            foreach (var parameter in parameters)
            {
                parameterChecks.Add(parameter.name, false);
            }
        }

        public OutfitData(int count, List<VRCExpressionParameters.Parameter> parameters)
        {
            name = $"{INITIAL_NAME}_{count}";
            uuid = GUID.Generate().ToString();

            foreach (var parameter in parameters)
            {
                if (parameter.name.StartsWith(Outfits.TOGGLE_PREFIX)) continue;
                parameterChecks.Add(parameter.name, false);
            }
        }

        public OutfitData(AnimatorState state, List<VRCExpressionParameters.Parameter> allParameters)
        {
            name = state.name;
            uuid = GUID.Generate().ToString();

            List<string> allParameterNames = new();

            foreach (var parameter in allParameters)
            {
                allParameterNames.Add(parameter.name);
            }

            foreach(var parameter in ((VRCAvatarParameterDriver)state.behaviours[0]).parameters)
            {
                if (parameter.name.StartsWith(Outfits.TOGGLE_PREFIX)) continue;
                if (!allParameterNames.Contains(parameter.name)) continue; // Deprecated parameter, not used anymore
                parameterChecks.Add(parameter.name, parameter.value == 1f);
            }

            // Add new parameters that have been created since then
            foreach(var parameter in allParameters)
            {
                if (parameter.name.StartsWith(Outfits.TOGGLE_PREFIX)) continue;
                if (parameterChecks.ContainsKey(parameter.name)) continue;
                parameterChecks.Add(parameter.name, false);
            }
        }

        public string Name { get => name; set => name = value; }
        public string UUID { get => uuid; }
        public Dictionary<string, bool> ParameterChecks { get => parameterChecks; }
        public List<VRCAvatarParameterDriver.Parameter> StateBehaviourParameters
        {
            get
            {
                var list = new List<VRCAvatarParameterDriver.Parameter>();

                foreach (var check in parameterChecks)
                {
                    list.Add(new VRCAvatarParameterDriver.Parameter()
                    {
                        type = VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Set,
                        value = check.Value ? 1f : 0f,
                        name = check.Key

                    });
                }
                return list;
            }
        }
    }

    public class Outfits : ScriptableObject
    {
        static readonly string OUTFIT_LAYER = "--- Jazzy Outfits ---";
        static public readonly string TOGGLE_PREFIX = "Jazzy/Outfits";

        VRCAvatarDescriptor selectedAvatar;
        AnimatorController fxAnimatorController;
        VRCExpressionParameters vrcParameters;
        List<VRCExpressionParameters.Parameter> jazzyParameters;

        List<OutfitData> outfits = new List<OutfitData>();
        List<string> errorMessages;
        bool isGathered = false;


        public void SelectAvatar(VRCAvatarDescriptor avatar)
        {
            selectedAvatar = avatar;
            GatherAvatarInfo();
        }

        void GatherAvatarInfo()
        {
            var controller = selectedAvatar.baseAnimationLayers[4].animatorController;
            fxAnimatorController = controller != null ? (AnimatorController)controller : null;
            vrcParameters = selectedAvatar.GetComponent<VRCAvatarDescriptor>().expressionParameters;
            jazzyParameters = Utils.AllJazzyParameters(vrcParameters.parameters);

            if (jazzyParameters.Count > 0 && outfits.Count <= 0)
            {
                outfits.Add(new OutfitData("Nude", jazzyParameters));
            }

            
        }

        public void GatherExistingOutfits()
        {
            if (isGathered) return;
            if (!Utils.AnimatorLayerExists(OUTFIT_LAYER, fxAnimatorController.layers, out int layerIndex)) return;

            var outfitLayer = fxAnimatorController.layers[layerIndex];

            foreach (var child in outfitLayer.stateMachine.states)
            {
                var state = child.state;
                if (state.behaviours.Length <= 0) continue;
                if (state.name == "Nude") continue;
                outfits.Add(new OutfitData(state, jazzyParameters));
            }
            isGathered = true;
        }

        public void ValidateAvatarInfo()
        {
            errorMessages = new List<string>();

            if (fxAnimatorController == null) errorMessages.Add("No FX controller is assigned to the avatar.");
            if (vrcParameters == null) errorMessages.Add("No expression parameters are assigned to the avatar.");
        }

        public void AddOutfit()
        {
            outfits.Add(new OutfitData(outfits.Count, jazzyParameters));
        }

        public void ClearOutfits()
        {
            outfits.Clear();
        }

        public void ApplyToAnimator()
        {
            if (Utils.AnimatorLayerExists(OUTFIT_LAYER, fxAnimatorController.layers, out int outfitLayer)) fxAnimatorController.RemoveLayer(outfitLayer);
            fxAnimatorController.AddLayer(OUTFIT_LAYER);
            var stateMachine = fxAnimatorController.layers[fxAnimatorController.layers.Length - 1].stateMachine;
            if (stateMachine == null) return;
            var idleState = stateMachine.AddState("Idle", new Vector3(30, 200, 0));

            foreach (var outfit in outfits)
            {
                var parameterName = $"{TOGGLE_PREFIX}/{outfit.Name}";
                if (!Utils.AnimatorParameterExists(parameterName, fxAnimatorController.parameters)) fxAnimatorController.AddParameter(parameterName, AnimatorControllerParameterType.Bool);


                if (stateMachine == null) return;

                var outfitState = stateMachine.AddState(outfit.Name);

                outfitState.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
                var behaviour = (VRCAvatarParameterDriver)outfitState.behaviours[0];
                behaviour.parameters = outfit.StateBehaviourParameters;

                outfitState.writeDefaultValues = false;

                var outfitToIdle = outfitState.AddTransition(idleState);
                var idleToOutfit = idleState.AddTransition(outfitState);

                outfitToIdle.duration = 0;
                outfitToIdle.exitTime = 0;
                outfitToIdle.hasExitTime = false;
                outfitToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, parameterName);

                idleToOutfit.duration = 0;
                idleToOutfit.exitTime = 0;
                idleToOutfit.hasExitTime = false;
                idleToOutfit.AddCondition(AnimatorConditionMode.If, 0, parameterName);
            }

            AnimatorControllerLayer[] layers = fxAnimatorController.layers;
            layers[fxAnimatorController.layers.Length - 1].defaultWeight = 1;
            fxAnimatorController.layers = layers;

            AssetDatabase.SaveAssets();
        }

        public void CreateVrcParameters()
        {
            List<VRCExpressionParameters.Parameter> parameters = new List<VRCExpressionParameters.Parameter>();

            foreach (var exisitingParameters in vrcParameters.parameters)
            {
                parameters.Add(exisitingParameters);
            }

            foreach (var outfit in outfits)
            {
                var vrcParameterName = $"{TOGGLE_PREFIX}/{outfit.Name}";
                if (vrcParameters.FindParameter(vrcParameterName) != null) continue; // Ignore if already exists

                parameters.Add(new VRCExpressionParameters.Parameter
                {
                    name = vrcParameterName,
                    valueType = VRCExpressionParameters.ValueType.Bool,
                    defaultValue = 0,
                    saved = false,
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

        public List<OutfitData> JazzyOutfits
        {
            get => outfits;
        }

        public bool IsGathered { get => isGathered; set => isGathered = value; }
    }
}
#endif