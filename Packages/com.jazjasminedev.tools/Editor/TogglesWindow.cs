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
    public class TogglesWindow : EditorWindow
    {
        Toggles toggles;
        Vector2 togglesScrollPosition;
        Vector2 errorMessagesScrollPosition;

        [MenuItem("Jazzy Tools/Toggles")]
        private static void Init()
        {
            TogglesWindow window = (TogglesWindow)GetWindow(typeof(TogglesWindow));
            window.Show();
            window.titleContent = new GUIContent("Jazzy Toggles");
            window.minSize = new Vector2(300, 600);
        }

        VRCAvatarDescriptor source;
        private void OnGUI()
        {
            if (toggles == null) toggles = CreateInstance<Toggles>();

            GUILayout.BeginHorizontal();
            source = (VRCAvatarDescriptor)EditorGUILayout.ObjectField("Avatar", source, typeof(VRCAvatarDescriptor), true);
            GUILayout.EndHorizontal();

            if (source == null) { GUILayout.Label(new GUIContent("You will need to insert your VRC Avatar first.")); return; }
            toggles.SelectAvatar(source);
            toggles.ValidateAvatarInfo();
            toggles.GatherExistingToggles();

            // --- TOGGLE LIST VIEW --- //

            togglesScrollPosition = GUILayout.BeginScrollView(togglesScrollPosition, GUILayout.ExpandHeight(true));


            toggles.JazzyToggles.ForEach(delegate (ToggleData toggle)
            {

                GUILayout.BeginHorizontal();
                toggle.Name = GUILayout.TextField(toggle.Name);
                toggle.DefaultValue = GUILayout.Toggle(toggle.DefaultValue, "Default");
                toggle.Saved = GUILayout.Toggle(toggle.Saved, "Saved");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent("GameObjects"));
                toggle.ObjectCountString = GUILayout.TextField(toggle.ObjectCountString, 2);
                GUILayout.EndHorizontal();

            // First prepare object list with empty objects:
            for (var i = 0; i < toggle.ObjectCount; i++)
                {
                    if (toggle.Objects.Count < i + 1)
                    {
                        toggle.Objects.Add(null);
                    }

                    toggle.Objects[i] = (GameObject)EditorGUILayout.ObjectField(toggle.Objects[i], typeof(GameObject), true);

                // Sets name to the initial object set if it's still the inital toggle name
                if (toggle.Name == ToggleData.INITIAL_NAME && toggle.Objects[0] != null) toggle.Name = toggle.Objects[0].name.Replace("_", "/");

                }
                GUILayout.Space(10);
                HorizontalLine(Color.gray);

            });
            GUILayout.EndScrollView();

            // --- BUTTONS --- //

            HorizontalLine(Color.green);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Toggle", GUILayout.Height(25)))
            {
                toggles.AddToggle();
            }
            if (GUILayout.Button("Clear all Toggles", GUILayout.Height(25)))
            {
                toggles.ClearToggles();
            }
            GUILayout.EndHorizontal();

            EditorGUI.BeginDisabledGroup(toggles.ErrorMessages.Count > 0);

            if (GUILayout.Button("Create Toggles", GUILayout.Height(40f)))
            {
                toggles.CreateAssetFolders();
                toggles.CreateAnimationClips();
                toggles.ApplyToAnimator();
                toggles.CreateVrcParameters();
                toggles.PostProcess();

                toggles.IsGathered = true;
            }

            EditorGUI.EndDisabledGroup();

            // --- ERRORS --- //

            if (toggles.ErrorMessages.Count <= 0) return;

            GUILayout.Label(new GUIContent("Errors"));
            errorMessagesScrollPosition = GUILayout.BeginScrollView(errorMessagesScrollPosition, GUILayout.ExpandHeight(true), GUILayout.MinHeight(150));
            toggles.ErrorMessages.ForEach(delegate (string message)
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