/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels.com/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace ProceduralPixels.BakeAO.Editor
{
    [UnityEditor.CustomEditor(typeof(BakeAORuntimeSettings))]
    internal class BakeAORuntimeSettingsEditor : UnityEditor.Editor
    {
        internal class Styles
        {
            public static readonly GUIContent DefaultBakeAOApplicationMode = EditorGUIUtility.TrTextContent("Default Apply Mode", "How to apply Bake AO component into renderer. You can select between MaterialPropertyBlock and Material Instance. Material Instance works faster when used in URP, MaterialPropertyBlock works faster in built-in render pipeline.");
        }

        private SerializedProperty defaultBakeAOApplyMode;

        private void OnEnable()
        {
            defaultBakeAOApplyMode = serializedObject.FindProperty("defaultApplyMode");
        }

        private void OnDisable()
        {
        }

        public override void OnInspectorGUI()
        {
            if (targets.Length > 1)
            {
                EditorGUILayout.HelpBox($"Multiple editing of {targets[0].GetType().Name} is not allowed.", MessageType.Warning);
                return;
            }

            ((BakeAORuntimeSettings)target).RefreshApplyMode();
            serializedObject.Update();

            var path = AssetDatabase.GetAssetPath(target);
            EditorGUILayout.HelpBox($"Settings below are stored in {path} in your project.", MessageType.Info);

            int currentDefault = defaultBakeAOApplyMode.enumValueIndex;
            BakeAOApplyModeEditor currentApplyMode = (BakeAOApplyModeEditor)currentDefault;

            if (GraphicsSettings.defaultRenderPipeline == null && currentApplyMode == BakeAOApplyModeEditor.MaterialInstance)
            {
                EditorGUILayout.HelpBox($"{BakeAOApplyModeEditor.MaterialInstance} is more performant when using Scriptable Render Pipeline. It is recommended to use {BakeAOApplyModeEditor.MaterialPropertyBlock} for built-in render pipeline", MessageType.Warning, true);
            }

            if (GraphicsSettings.defaultRenderPipeline != null && currentApplyMode == BakeAOApplyModeEditor.MaterialPropertyBlock)
            {
                EditorGUILayout.HelpBox($"{BakeAOApplyModeEditor.MaterialPropertyBlock} is more performant when using built-in render pipeline. It is recommended to use {BakeAOApplyModeEditor.MaterialInstance} for Scriptable Render Pipeline", MessageType.Warning, true);
            }

            int selectedValue = (int)(BakeAOApplyModeEditor)EditorGUILayout.EnumPopup(Styles.DefaultBakeAOApplicationMode, (BakeAOApplyModeEditor)currentDefault);
            
            if (currentDefault != selectedValue)
            {
                defaultBakeAOApplyMode.enumValueIndex = selectedValue;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
                ((BakeAORuntimeSettings)target).RefreshApplyMode();
                serializedObject.Update();
            }
        }

        internal static void SetDefaultApplyMode(BakeAORuntimeSettings settings, BakeAOApplyModeEditor mode)
        {
            SerializedObject so = new SerializedObject(settings);
            var defaultApplyModeProperty = so.FindProperty("defaultApplyMode");
            defaultApplyModeProperty.enumValueIndex = (int)mode;
            so.ApplyModifiedProperties();
            so.Update();
            EditorUtility.SetDirty(settings);

            settings.RefreshApplyMode();
        }
    }
}