/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels.com/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProceduralPixels.BakeAO.Editor
{
    [FilePath("BakeAOPreferences.asset", FilePathAttribute.Location.PreferencesFolder)]
    internal class BakeAOPreferences : ScriptableSingleton<BakeAOPreferences>
    {
        public enum BakingPriority
        {
            VeryLow = 0,
            Low = 1,
            Medium = 2,
            High = 3,
            VeryHigh = 4
        }

        [SerializeField] internal BakingPriority bakingPriority = BakingPriority.Medium;
        [SerializeField] internal bool showSubmeshSelectionInBakingSettings = false;
        [SerializeField] internal int maxMemoryUsageInMB = 512;
        [SerializeField] internal bool showApplyModeOption = false;

        public float GetBakingFrameTime()
        {
            switch (bakingPriority)
            {
                case BakingPriority.VeryLow:
                    return 0.0001f;
                case BakingPriority.Low:
                    return 0.001f;
                case BakingPriority.Medium:
                    return 0.005f;
                case BakingPriority.High:
                    return 0.016f;
                case BakingPriority.VeryHigh:
                    return 0.1f;
                default:
                    throw new InvalidEnumArgumentException("Unsupported enum value");
            }
        }

        void OnDisable()
        {
            Save();
        }

        public void Save()
        {
            Save(true);
        }

        internal SerializedObject GetSerializedObject()
        {
            return new SerializedObject(this);
        }

        public void DrawBakingPriorityProperty()
        {
            var serializedObject = GetSerializedObject();
            EditorGUI.BeginChangeCheck();

            var bakingPriorityProperty = serializedObject.FindProperty("bakingPriority");
            bakingPriorityProperty.enumValueIndex = (int)(BakeAOPreferences.BakingPriority)EditorGUILayout.EnumPopup(BakeAOPreferencesProvider.Styles.Priority, (BakeAOPreferences.BakingPriority)bakingPriorityProperty.enumValueIndex);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }
        }
    }

    internal class BakeAOPreferencesProvider : SettingsProvider
    {
        SerializedObject serializedObject;
        SerializedProperty bakingPriorityProperty;
        SerializedProperty maxMemoryUsageProperty;
        SerializedProperty showSubmeshSelectionInBakingSettingsProperty;
        SerializedProperty showApplyModeInAdvancedBakeAOOptions;

        public class Styles
        {
            public static readonly GUIContent Priority = EditorGUIUtility.TrTextContent("Baking Priority", "Use low priority if you want to keep the editor performance smooth. Highest priority provides the fastest baking speed, but will make unity editor laggy during the baking process.");
            public static readonly GUIContent MaximumMemoryUsageWhenBaking = EditorGUIUtility.TrTextContent("Max memory usage in MB", "Maximum memory usage during the baking process. When more than one texture is queued for baking, Bake AO will try to use available memory before saving texture assets. Keep this value low if you want to limit the memory usage and quickly save baked textures into assets.");
            public static readonly GUIContent ShowSubmeshSelectionInBakingSettings = EditorGUIUtility.TrTextContent("Show submesh selection in baking settings", "When enabled, you will be able to select a set of target submeshes to bake the texture in the baking settings. ");
            public static readonly GUIContent ShowApplyMode = EditorGUIUtility.TrTextContent("Show apply mode in Bake AO component", "Shows the apply mode option for each Bake AO component. The option is available under Advanced section.");
        }

        public BakeAOPreferencesProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords)
        {
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            BakeAOPreferences.instance.Save();
            serializedObject = BakeAOPreferences.instance.GetSerializedObject();
            bakingPriorityProperty = serializedObject.FindProperty(nameof(BakeAOPreferences.bakingPriority));
            maxMemoryUsageProperty = serializedObject.FindProperty(nameof(BakeAOPreferences.maxMemoryUsageInMB));
            showSubmeshSelectionInBakingSettingsProperty = serializedObject.FindProperty(nameof(BakeAOPreferences.showSubmeshSelectionInBakingSettings));
            showApplyModeInAdvancedBakeAOOptions = serializedObject.FindProperty(nameof(BakeAOPreferences.showApplyModeOption));
        }

        public override void OnGUI(string searchContext)
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            {
                var originalLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = originalLabelWidth * 1.7f;

                EditorGUILayout.LabelField("Baking settings", EditorStyles.boldLabel);
                bakingPriorityProperty.enumValueIndex = (int)(BakeAOPreferences.BakingPriority)EditorGUILayout.EnumPopup(Styles.Priority, (BakeAOPreferences.BakingPriority)bakingPriorityProperty.enumValueIndex);
                maxMemoryUsageProperty.intValue = EditorGUILayout.IntField(Styles.MaximumMemoryUsageWhenBaking, maxMemoryUsageProperty.intValue);
                maxMemoryUsageProperty.intValue = Mathf.Clamp(maxMemoryUsageProperty.intValue, 0, 8096);
                if (BakingManager.instance.EstimatedReservedMemory > (long)maxMemoryUsageProperty.intValue * 1024L * 1024L)
                    EditorGUILayout.HelpBox("Bake AO does not guarantee that the memory limit will not be exceeded. Baking higher resolutions will result in higher memory usage.", MessageType.Warning, true);
                EditorGUILayout.HelpBox($"Current memory used for baking: {BakingManager.instance.EstimatedReservedMemory / (1024L * 1024L):N} MB", MessageType.Info, true);
                EditorGUILayout.Space();
                showSubmeshSelectionInBakingSettingsProperty.boolValue = EditorGUILayout.Toggle(Styles.ShowSubmeshSelectionInBakingSettings, showSubmeshSelectionInBakingSettingsProperty.boolValue);
                showApplyModeInAdvancedBakeAOOptions.boolValue = EditorGUILayout.Toggle(Styles.ShowApplyMode, showApplyModeInAdvancedBakeAOOptions.boolValue);
                EditorGUIUtility.labelWidth = originalLabelWidth;
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
                BakeAOPreferences.instance.Save();
            }
        }

        [SettingsProvider]
        public static SettingsProvider CreateBakeAOSettingProvider()
        {
            var provider = new BakeAOPreferencesProvider("Preferences/Bake AO", SettingsScope.User, GetSearchKeywordsFromGUIContentProperties<Styles>());
            return provider;
        }
    }
}