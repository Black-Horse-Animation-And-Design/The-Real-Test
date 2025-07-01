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
    [UnityEditor.CustomEditor(typeof(BakeAOSettings))]
    internal class BakeAOSettingsEditor : UnityEditor.Editor
    {
        internal class Styles
        {
            public static readonly GUIContent ShadersThatSupportBakeAO = EditorGUIUtility.TrTextContent("Shaders that support BakeAO", "Shaders that are recognised as supported by Bake AO. Those shaders support BakeAO properties.");
            public static readonly GUIContent ShaderRemap = EditorGUIUtility.TrTextContent("Shader remap", "Shaders that can be swapped by BakeAO in the materials to make them support Bake AO properties");
            public static readonly GUIContent LayersInteraction = EditorGUIUtility.TrTextContent("Layers interaction", "Defines which layer is affected by which layer when determining occluders from the scene.");
            public static readonly GUIContent OtherOptions = EditorGUIUtility.TrTextContent("Other Options", "Other options for Bake AO.");
            public static readonly GUIContent ShowApplyModeOption = EditorGUIUtility.TrTextContent("Show Apply Mode option", "Show Bake AO apply mode setting in the Bake AO component. You will be able to see the option under \"Advanced\" section of the Bake AO component.");
            public static readonly GUIContent BakeMaterialsForStaticGameObjects = EditorGUIUtility.TrTextContent("Bake materials for static gameobjects", "When it's enabled, the Bake AO component will be removed from a static game objects when building the scene. Bake AO properties will be applied to the materials of the renderer (copy of the original materials)");
        }

        SerializedProperty shadersThatSupportBakeAOProperty;
        SerializedProperty shaderRemapProperty;
        SerializedProperty layersInteractionProperty;
        SerializedProperty bakeMaterialsForStatic;

        private bool layersInteractionFoldoutState = false;
        private bool otherOptionsFoldoutState = false;

        private void OnEnable()
        {
            shadersThatSupportBakeAOProperty = serializedObject.FindProperty(nameof(BakeAOSettings.shadersThatSupportBakeAO));
            shaderRemapProperty = serializedObject.FindProperty(nameof(BakeAOSettings.shaderRemap));
            layersInteractionProperty = serializedObject.FindProperty(nameof(BakeAOSettings.layersInteraction));
            bakeMaterialsForStatic = serializedObject.FindProperty(nameof(BakeAOSettings.bakeMaterialsForStaticGameObjects));
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

            var path = AssetDatabase.GetAssetPath(target);
            EditorGUILayout.HelpBox($"Settings below are stored in {path} in your project.", MessageType.Info);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(shadersThatSupportBakeAOProperty, Styles.ShadersThatSupportBakeAO);
            EditorGUILayout.PropertyField(shaderRemapProperty, Styles.ShaderRemap);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
                EditorUtility.SetDirty(target);
            }

            bool isDirty = false;

            layersInteractionFoldoutState = EditorGUILayout.BeginFoldoutHeaderGroup(layersInteractionFoldoutState, Styles.LayersInteraction);
            EditorGUI.indentLevel++;

            if (layersInteractionFoldoutState)
            {
                // Get all layer names up to the maximum of 32 layers
                string[] layerNames = new string[32];
                for (int i = 0; i < 32; i++)
                {
                    layerNames[i] = LayerMask.LayerToName(i);
                    if (string.IsNullOrEmpty(layerNames[i]))
                        layerNames[i] = $"Layer {i}";
                }

                for (int i = 0; i < 32; i++)
                {
                    // Draw the LayerMask field
                    int currentMaskValue = (target as BakeAOSettings).layersInteraction[i];
                    int newMaskValue = EditorGUILayout.MaskField(layerNames[i], currentMaskValue, layerNames);

                    if (currentMaskValue != newMaskValue)
                    {
                        (target as BakeAOSettings).layersInteraction[i] = newMaskValue;
                        isDirty |= true;
                    }
                }
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUI.BeginChangeCheck();
            float labelWidth = EditorGUIUtility.labelWidth;
            otherOptionsFoldoutState = EditorGUILayout.BeginFoldoutHeaderGroup(otherOptionsFoldoutState, Styles.OtherOptions);
            if (otherOptionsFoldoutState)
            {
                EditorGUIUtility.labelWidth *= 1.9f;
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(bakeMaterialsForStatic, Styles.BakeMaterialsForStaticGameObjects);
                if (bakeMaterialsForStatic.boolValue && GraphicsSettings.defaultRenderPipeline == null)
                {
                    EditorGUILayout.HelpBox("Baking materials for static objects can slow down the rendering when using built-in render pipeline. This option is recommended for URP.", MessageType.Warning, true);
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUIUtility.labelWidth = labelWidth;
            isDirty |= EditorGUI.EndChangeCheck();

            if (isDirty)
            {
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
                EditorUtility.SetDirty(target);
            }
        }
    }
}