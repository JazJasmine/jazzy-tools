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
    public class OutfitsWindow : EditorWindow
    {
        Outfits outfits;
        Vector2 errorMessagesScrollPosition;
        Dictionary<string, bool> showOutfitViews = new Dictionary<string, bool>();

        [MenuItem("Jazzy Tools/Outfits")]
        private static void Init()
        {
            OutfitsWindow window = (OutfitsWindow)GetWindow(typeof(OutfitsWindow));
            window.Show();
            window.titleContent = new GUIContent("Jazzy Outfits");
            window.minSize = new Vector2(300, 600);
        }

        VRCAvatarDescriptor source;
        private void OnGUI()
        {
            if (outfits == null) outfits = CreateInstance<Outfits>();

            GUILayout.BeginHorizontal();
            source = (VRCAvatarDescriptor)EditorGUILayout.ObjectField("Avatar", source, typeof(VRCAvatarDescriptor), true);
            GUILayout.EndHorizontal();

            if (source == null) { GUILayout.Label(new GUIContent("You will need to insert your VRC Avatar first.")); return; }
            outfits.SelectAvatar(source);
            outfits.ValidateAvatarInfo();
            outfits.GatherExistingOutfits();

            // --- OUTFITS LIST VIEW --- //

            outfits.JazzyOutfits.ForEach(delegate (OutfitData outfit)
            {
                if (!showOutfitViews.ContainsKey(outfit.UUID)) { showOutfitViews.Add(outfit.UUID, false); }

                showOutfitViews[outfit.UUID] = EditorGUILayout.Foldout(showOutfitViews[outfit.UUID], outfit.Name);

                if(showOutfitViews[outfit.UUID])
                {
                    GUILayout.BeginHorizontal();
                    outfit.Name = GUILayout.TextField(outfit.Name);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginVertical();
                    var iterationCopy = new Dictionary<string, bool>(outfit.ParameterChecks);
                    foreach(var check in iterationCopy)
                    {
                        outfit.ParameterChecks[check.Key] = GUILayout.Toggle(check.Value, check.Key.Replace($"{Toggles.TOGGLE_PREFIX}/", ""));
                    }
                    GUILayout.EndVertical();
                }


            });

            // --- BUTTONS --- //

            HorizontalLine(Color.green);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Outfit", GUILayout.Height(25)))
            {
                outfits.AddOutfit();
            }
            if (GUILayout.Button("Clear all Toggles", GUILayout.Height(25)))
            {
                showOutfitViews = new Dictionary<string, bool>();
                outfits.ClearOutfits();
            }
            GUILayout.EndHorizontal();

            EditorGUI.BeginDisabledGroup(outfits.ErrorMessages.Count > 0);

            if (GUILayout.Button("Create Outfits", GUILayout.Height(40f)))
            {
                outfits.ApplyToAnimator();
                outfits.CreateVrcParameters();
                outfits.PostProcess();

                outfits.IsGathered = true;
            }

            EditorGUI.EndDisabledGroup();

            // --- ERRORS --- //

            if (outfits.ErrorMessages.Count <= 0) return;

            GUILayout.Label(new GUIContent("Errors"));
            errorMessagesScrollPosition = GUILayout.BeginScrollView(errorMessagesScrollPosition, GUILayout.ExpandHeight(true), GUILayout.MinHeight(150));
            outfits.ErrorMessages.ForEach(delegate (string message)
            {
                EditorGUILayout.HelpBox(message, MessageType.Error);
            });
            GUILayout.EndScrollView();


        }

        // GUI Helpers
        static void HorizontalLine(Color color)
        {
            var horizontalLine = new GUIStyle()
            {
                normal = { background = EditorGUIUtility.whiteTexture },
                margin = new RectOffset(0, 0, 10, 10),
                fixedHeight = 1
            };

            var c = GUI.color;
            GUI.color = color;
            GUILayout.Box(GUIContent.none, horizontalLine);
            GUI.color = c;
        }
    }
}
#endif