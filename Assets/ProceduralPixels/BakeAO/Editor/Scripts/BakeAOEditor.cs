/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels.com/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using static ProceduralPixels.BakeAO.Editor.BakeAOUtils;
using System.Linq;

namespace ProceduralPixels.BakeAO.Editor
{
    [CustomEditor(typeof(GenericBakeAO), true), CanEditMultipleObjects]
    internal class BakeAOEditor : UnityEditor.Editor
    {
        private struct MaterialsData
        {
            public bool containsAnyNonSupportedMaterial;
            public bool canAnyNonSupportedBeConverted;

            public void Reset()
            {
                containsAnyNonSupportedMaterial = false;
                canAnyNonSupportedBeConverted = false;
            }

            public void Reevaluate(IEnumerable<UnityEngine.Object> objs)
            {
                Reset();
                Evaluate(objs);
            }

            public void Evaluate(IEnumerable<UnityEngine.Object> objs)
            {
                foreach (var o in objs)
                    Evaluate(o);
            }

            public void Evaluate(UnityEngine.Object obj)
            {
                if (BakeAOUtils.TryGetMaterials(obj, out var materials))
                {
                    foreach (var material in materials)
                    {
                        if (material == null)
                            continue;

                        containsAnyNonSupportedMaterial |= !BakeAOSettings.Instance.IsMaterialSupported(material);
                        canAnyNonSupportedBeConverted |= BakeAOSettings.Instance.CanUpdateMaterial(material);
                    }
                }
            }

            public void FixMaterials(IEnumerable<UnityEngine.Object> objs)
            {
                foreach (var o in objs)
                    FixMaterials(o);

                Reevaluate(objs);
            }

            public void FixMaterials(UnityEngine.Object obj)
            {
                if (BakeAOUtils.TryGetMaterials(obj, out var materials))
                {
                    foreach (var material in materials)
                    {
                        if (material == null)
                            continue;

                        if (!BakeAOSettings.Instance.IsMaterialSupported(material) && BakeAOSettings.Instance.CanUpdateMaterial(material))
                        {
                            Undo.RecordObject(material, "Bake AO - Update Materials");
                            BakeAOSettings.Instance.UpdateMaterial(material);
                            EditorUtility.SetDirty(material);
                        }
                    }
                }
            }
        }

        SerializedProperty modeProperty;
        SerializedProperty ambientOcclusionTextureProperty;
        SerializedProperty ambientOcclusionStrengthProperty;
        SerializedProperty occlusionUVSetProperty;
        SerializedProperty applyOcclusionIntoDiffuseProperty;
        SerializedProperty rendererRefProperty;
        private MaterialsData materialsData;

        GenericBakingSetup genericBakingSetup;
        GenericBakingSetupEditor bakingSetupEditor;

        List<AOTextureSearch.TextureSearchResult> foundModelTextures = new List<AOTextureSearch.TextureSearchResult>();

        private void OnEnable()
        {
            modeProperty = serializedObject.FindProperty("mode");
            ambientOcclusionTextureProperty = serializedObject.FindProperty("ambientOcclusionTexture");
            ambientOcclusionStrengthProperty = serializedObject.FindProperty("occlusionStrength");
            occlusionUVSetProperty = serializedObject.FindProperty("occlusionUVSet");
            applyOcclusionIntoDiffuseProperty = serializedObject.FindProperty("applyOcclusionIntoDiffuse");
            rendererRefProperty = serializedObject.FindProperty("rendererRef");
            UnityEditor.EditorApplication.update += OnInspectorUpdate;

            if (BakeAOSessionSettings.instance.LastUsedBakingSetup == null)
                genericBakingSetup = GenericBakingSetup.DefaultFromPresets;
            else
                genericBakingSetup = BakeAOSessionSettings.instance.LastUsedBakingSetup.Clone();

            genericBakingSetup.hideFlags = HideFlags.DontSave;

            bakingSetupEditor = (GenericBakingSetupEditor)CreateEditor(genericBakingSetup, typeof(GenericBakingSetupEditor));

            materialsData.Reevaluate(targets);

            for (int i = 0; i < targets.Length; i++)
            {
                var bakeAO = targets[i] as GenericBakeAO;
                if (bakeAO.HasInvalidTexture())
                    BakeAOUtils.RefreshAllBakeAOComponents();
            }

            Undo.undoRedoPerformed += UndoRedoPerformed;

            AOTextureSearch.instance.OnInitializationFinished += RefreshFoundModelTextures;
            RefreshFoundModelTextures();
        }

        private void OnDisable()
        {
            DestroyImmediate(genericBakingSetup);
            AOTextureSearch.instance.OnInitializationFinished -= RefreshFoundModelTextures;
            ambientOcclusionTextureProperty = null;
            Undo.undoRedoPerformed -= UndoRedoPerformed;
            UnityEditor.EditorApplication.update -= OnInspectorUpdate;
        }

        private void RefreshFoundModelTextures()
        {
            if (!AOTextureSearch.instance.IsInitialized)
                return;

            if (targets.Length == 1)
            {
                if (BakeAOUtils.TryGetMesh(target, out Mesh mesh))
                {
                    foundModelTextures = new List<AOTextureSearch.TextureSearchResult>();
                    AOTextureSearch.instance.TryFindAllAOTextures(mesh, foundModelTextures);
                }
            }

            Repaint();
        }

        public override bool RequiresConstantRepaint()
        {
            return !AOTextureSearch.instance.IsInitialized;  
        }

        public void OnInspectorUpdate()
        {
            if (RequiresConstantRepaint())
                Repaint();
        }

        public bool DrawInitializationGUIIfNeeded()
        {
            if (!AOTextureSearch.instance.IsInitialized)
            {
                AOTextureSearch.instance.WaitForInitialized();
                var progressBarRect = EditorGUILayout.GetControlRect();
                EditorGUI.ProgressBar(progressBarRect, AOTextureSearch.instance.InitializationProgress.Value, "Initializing...");
                return true;
            }

            return false;
        }

        public override void OnInspectorGUI()
        {
            canBake = true;

            if (targets.Length == 1)
                DrawSingleObjectInspector();
            else
                DrawMultipleObjectInspector();
        }

        public bool IsComponentValid(GenericBakeAO bakeAO)
        {
            var meshRenderer = bakeAO.GetComponent<MeshRenderer>();
            var skinnedMeshRenderer = bakeAO.GetComponent<SkinnedMeshRenderer>();
            var meshFilter = bakeAO.GetComponent<MeshFilter>();

            return ((meshFilter != null && meshRenderer != null) || (skinnedMeshRenderer != null));
        }

        public bool IsPreset(GenericBakeAO bakeAO)
        {
            bool sceneOfThisObjectDoesNotExist = !bakeAO.gameObject.scene.isLoaded;
            return sceneOfThisObjectDoesNotExist;
        }

        private const float PropertyButtonSize = 40.0f;
        private const float DotButtonSize = 21.0f;
        private const float BakePostprocessButtonSize = 135.0f;
        private const float MultiBakeOptionSize = 105.0f;
        private const float Margin = 4;

        public static bool bakingFoldoutState = false;
        public static bool advancedFoldoutState = false;

        private bool canBake = true;

        #region SINGLE COMPONENT EDITOR

        private void DrawSingleObjectInspector()
        {
            bool isComponentValid = IsComponentValid(target as GenericBakeAO);
            bool isPresetEditor = IsPreset(target as GenericBakeAO);

            if (!isComponentValid && !isPresetEditor)
            {
                EditorGUILayout.HelpBox("BakeAO component needs MeshRenderer with MeshFilter or SkinnedMeshRenderer components to work correctly.", MessageType.Error, true);
                return;
            }

            Rect controlRect = EditorGUILayout.GetControlRect();
            Rect propertyRect = new Rect(controlRect.x, controlRect.y, controlRect.width - PropertyButtonSize - DotButtonSize - Margin, controlRect.height);
            Rect findButtonRect = new Rect(controlRect.x + controlRect.width - PropertyButtonSize - DotButtonSize, controlRect.y, PropertyButtonSize, controlRect.height);
            Rect dotsButtonRect = new Rect(controlRect.x + controlRect.width - DotButtonSize, controlRect.y, DotButtonSize, controlRect.height);

            bool isChanged = false;

            EditorGUI.BeginChangeCheck();

            EditorGUI.PropertyField(propertyRect, ambientOcclusionTextureProperty);
            EditorGUILayout.PropertyField(ambientOcclusionStrengthProperty);
            advancedFoldoutState = EditorGUILayout.Foldout(advancedFoldoutState, "Advanced");
            if (advancedFoldoutState)
            {
                EditorGUI.indentLevel++;

                if (BakeAOPreferences.instance.showApplyModeOption)
                    EditorGUILayout.PropertyField(modeProperty);
                EditorGUILayout.PropertyField(occlusionUVSetProperty);
                EditorGUILayout.PropertyField(applyOcclusionIntoDiffuseProperty);

                materialsData.Reevaluate(targets);

                DrawMaterialSupportHelpBoxes();

                EditorGUI.indentLevel--;
            }

            if (DrawInitializationGUIIfNeeded())
            {
                FinalizeGUIEditor();
                return;
            }

            EditorGUI.BeginDisabledGroup(foundModelTextures.Count == 0 || isPresetEditor);

            if (GUI.Button(findButtonRect, "Next"))
                TryFindAndSetAOTexture();

            if (GUI.Button(dotsButtonRect, "..."))
                TryFindAndSetAOTextures();

            EditorGUI.EndDisabledGroup();

            if (isPresetEditor)
            {
                if (rendererRefProperty.objectReferenceValue != null)
                {
                    // Presets shoudn't have any renderer reference set.
                    rendererRefProperty.objectReferenceValue = null;
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    serializedObject.Update();
                }

                FinalizeGUIEditor();

                return;
            }

            var foldoutRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            HorizontalRectLayout layout = new HorizontalRectLayout(foldoutRect);
            UnityEditor.Presets.PresetSelector.DrawPresetButton(layout.GetFromRight(21), new UnityEngine.Object[] { genericBakingSetup });
            GUIUtility.HelpButton(layout.GetFromRight(21), "https://proceduralpixels.com/BakeAO/Documentation/BakingParameters");
            layout.GetFromRight(4); // Margin
            BakeAOUtils.FoldoutWithButton(layout.GetReminder(), ref bakingFoldoutState, "Baking", "...", DotButtonSize, () =>
            {
                DoBakingContextMenu(targets.Cast<GenericBakeAO>());
            });

            if (bakingFoldoutState)
            {
                EditorGUI.indentLevel++;
                DrawSingleObjectBakingEditor();
                EditorGUI.indentLevel--;
            }

            FinalizeGUIEditor();

            void FinalizeGUIEditor()
            {
                isChanged |= EditorGUI.EndChangeCheck();

                bakingSetupEditor.serializedObject.ApplyModifiedProperties();
                bakingSetupEditor.serializedObject.Update();

                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();

                if (isChanged)
                    (target as GenericBakeAO).UpdateAmbientOcclusionProperties();
            }

            void TryFindAndSetAOTexture()
            {
                var component = target as GenericBakeAO;

                int indexOf = foundModelTextures.FindIndex(r => r.aoTexture == component.AmbientOcclusionTexture);
                indexOf++;
                indexOf = indexOf % foundModelTextures.Count;
                ambientOcclusionTextureProperty.objectReferenceValue = foundModelTextures[indexOf].aoTexture;
                occlusionUVSetProperty.enumValueIndex = (int)foundModelTextures[indexOf].bakingSetup.originalMeshes[0].uv;

                var so = ambientOcclusionStrengthProperty.serializedObject;
                so.ApplyModifiedProperties();
                so.Update();

                component.Refresh();
            }
        }

        private void DrawMaterialSupportHelpBoxes()
        {
            if (materialsData.containsAnyNonSupportedMaterial)
            {
                if (targets.Length == 1)
                    EditorGUILayout.HelpBox("The shader used by this renderer is not fully supported by Bake AO. Please make sure that the shaders used support Bake AO.", MessageType.Warning, true);
                else
                    EditorGUILayout.HelpBox("Some of the shaders used by renderers are not fully supported by Bake AO. Please make sure that all the shaders used support Bake AO.", MessageType.Warning, true);
                    // TODO: Draw here a button or list of not supported shaders used or something like that.

                if (materialsData.canAnyNonSupportedBeConverted)
                {
                    EditorGUILayout.HelpBox("Some of the materials can be automatically updated. Use button below to update their shaders.", MessageType.Warning, true);

                    if (GUILayout.Button("Update materials shaders"))
                        materialsData.FixMaterials(targets);
                }
                else
                {
                    if (GUILayout.Button("Open documentation"))
                        Application.OpenURL("https://proceduralpixels.com/BakeAO/Documentation/CustomShaders");
                }

            }
        }

        private void DrawSingleObjectBakingEditor()
        {
            var bakeAO = target as GenericBakeAO;
            bakingSetupEditor.SetContext(bakeAO.GetComponent<Renderer>());

            DrawBakingSetupSettings();

            bool haveUVSet = true;

            if (BakeAOUtils.TryGetMesh(target, out Mesh meshToBake))
            {
                var genericBakingSetup = (GenericBakingSetup)bakingSetupEditor.target;
                haveUVSet = meshToBake.DoesHaveUVSet(genericBakingSetup.uvChannel);
                if (!haveUVSet)
                {
                    if (genericBakingSetup.uvChannel == UVChannel.UV1)
                    {
                        EditorGUILayout.HelpBox($"This mesh does not contain {genericBakingSetup.uvChannel} data. Select different UV set or generate this UV by using Generate Lightmap UV's option in the model import settings.", MessageType.Error, true);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox($"This mesh does not contain {genericBakingSetup.uvChannel} data. Please select different UV set.", MessageType.Error, true);
                    }
                }
            }

            EditorGUI.BeginDisabledGroup(!canBake || !haveUVSet);
            Rect r = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            Rect bakeButtonRect = new Rect(r.position, new Vector2(r.width - BakePostprocessButtonSize + 14, r.height));
            Rect postprocessActionRect = new Rect(r.position + new Vector2(r.width - BakePostprocessButtonSize, 0), new Vector2(BakePostprocessButtonSize, r.height));

            EditorGUI.PropertyField(postprocessActionRect, bakingSetupEditor.textureAssetPostprocessActionProperty, GUIContent.none);

            if (GUI.Button(bakeButtonRect, "Bake AO"))
            {
                BakeTextureForObject(bakeAO, null, () =>
                {
                    if (this != null && target != null)
                        RefreshFoundModelTextures();
                });
            }
        }

        GenericMenu textureSelectionMenu;

        private void TryFindAndSetAOTextures()
        {
            var component = target as GenericBakeAO;

            Mesh mesh;
            if (!BakeAOUtils.TryGetMesh(component, out mesh))
                return;

            List<AOTextureSearch.TextureSearchResult> textures = new List<AOTextureSearch.TextureSearchResult>();
            if (AOTextureSearch.instance.TryFindAllAOTextures(mesh, textures))
            {
                textureSelectionMenu = new GenericMenu();

                for (int i = 0; i < textures.Count; i++)
                {
                    var searchRecord = textures[i];
                    textureSelectionMenu.AddItem(new GUIContent($"{(i+1)}: {searchRecord.aoTexture.name}"), searchRecord.aoTexture == component.AmbientOcclusionTexture, (obj) =>
                    {
                        var searchRes = obj as AOTextureSearch.TextureSearchResult;
                        ambientOcclusionTextureProperty.objectReferenceValue = searchRes.aoTexture;
                        occlusionUVSetProperty.enumValueIndex = (int)searchRes.bakingSetup.originalMeshes[0].uv;
                        ambientOcclusionTextureProperty.serializedObject.ApplyModifiedProperties();
                        ambientOcclusionTextureProperty.serializedObject.Update();

                        foreach (var target in targets)
                        {
                            if (target == null)
                                continue;

                            var tempComponent = target as GenericBakeAO;
                            tempComponent.Refresh();
                        }

                    }, searchRecord);
                }
                textureSelectionMenu.ShowAsContext();
            }
        }

        #endregion

        #region MULTIPLE COMPONENT EDITOR

        GenericMenu findMenu;

        private void DrawMultipleObjectInspector()
        {
            bool isPresetEditor = IsPreset(targets[0] as GenericBakeAO);

            bakingSetupEditor.SetContext(null);

            Rect controlRect = EditorGUILayout.GetControlRect(); 
            Rect propertyRect = new Rect(controlRect.x, controlRect.y, controlRect.width - PropertyButtonSize - Margin, controlRect.height);
            Rect findButtonRect = new Rect(controlRect.x + controlRect.width - PropertyButtonSize, controlRect.y, PropertyButtonSize, controlRect.height);

            bool isChanged = false;

            EditorGUI.BeginChangeCheck();

            EditorGUI.PropertyField(propertyRect, ambientOcclusionTextureProperty);
            EditorGUILayout.PropertyField(ambientOcclusionStrengthProperty);
            advancedFoldoutState = EditorGUILayout.Foldout(advancedFoldoutState, "Advanced");

            if (advancedFoldoutState)
            { 
                EditorGUI.indentLevel++;

                if (BakeAOPreferences.instance.showApplyModeOption)
                    EditorGUILayout.PropertyField(modeProperty);

                EditorGUILayout.PropertyField(occlusionUVSetProperty);
                EditorGUILayout.PropertyField(applyOcclusionIntoDiffuseProperty);

                DrawMaterialSupportHelpBoxes();

                EditorGUI.indentLevel--;
            }

            if (DrawInitializationGUIIfNeeded())
            {
                FinalizeGUIEditor();
                return;
            }

            EditorGUI.BeginDisabledGroup(isPresetEditor);
            if (GUI.Button(findButtonRect, "Find"))
            {
                findMenu = new GenericMenu();
                findMenu.AddItem(new GUIContent("Find for all missing"), false, FindTextureForAllMissing);
                findMenu.ShowAsContext();
            }
            EditorGUI.EndDisabledGroup();

            if (isPresetEditor)
            {
                rendererRefProperty.objectReferenceValue = null;
                FinalizeGUIEditor();
                return;
            }

            var foldoutRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            HorizontalRectLayout layout = new HorizontalRectLayout(foldoutRect);
            UnityEditor.Presets.PresetSelector.DrawPresetButton(layout.GetFromRight(21), new UnityEngine.Object[] { genericBakingSetup });
            GUIUtility.HelpButton(layout.GetFromRight(21), "https://proceduralpixels.com/BakeAO/Documentation/BakingParameters");
            layout.GetFromRight(4); // margin
            BakeAOUtils.FoldoutWithButton(layout.GetReminder(), ref bakingFoldoutState, "Baking", "...", DotButtonSize, () =>
            {
                DoBakingContextMenu(targets.Cast<GenericBakeAO>());
            });

            if (bakingFoldoutState)
            {
                EditorGUI.indentLevel++;
                DrawMultipleObjectBakingEditor();
                EditorGUI.indentLevel--;
            }

            FinalizeGUIEditor();

            void FinalizeGUIEditor()
            {
                isChanged |= EditorGUI.EndChangeCheck();

                bakingSetupEditor.serializedObject.ApplyModifiedProperties();
                bakingSetupEditor.serializedObject.Update();

                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();

                if (isChanged)
                    RefreshAllTargets();
            }
        }

        private void DoBakingContextMenu(IEnumerable<GenericBakeAO> components)
        {
            var bakingContextMenu = new GenericMenu();

            bool drawDisabledFetchFromTexture = true;

            if (components.Count() == 1)
            {
                var component = components.First();
                var setup = genericBakingSetup.Clone();
                if (AOTextureSearch.instance.TryGetBakingSetupFromTexture(component.AmbientOcclusionTexture, ref setup))
                {
                    drawDisabledFetchFromTexture = false;
                    bakingContextMenu.AddItem(new GUIContent("Fetch from texture"), false, () =>
                    {
                        if (BakeAOPreferences.instance.showSubmeshSelectionInBakingSettings)
                        {
                            genericBakingSetup.occluderSubmeshFlags = setup.occluderSubmeshFlags;
                            genericBakingSetup.targetSubmeshFlags = setup.targetSubmeshFlags;
                        }
                        else
                        {
                            genericBakingSetup.occluderSubmeshFlags = -1;
                            genericBakingSetup.targetSubmeshFlags = -1;
                        }

                        genericBakingSetup.uvChannel = setup.uvChannel;
                        genericBakingSetup.quality = setup.quality;
                        genericBakingSetup.contextBakingSettings = setup.contextBakingSettings;
                        BakeAOSessionSettings.instance.LastUsedBakingSetup = genericBakingSetup.Clone();
                    });
                }
            }

            if (drawDisabledFetchFromTexture)
                bakingContextMenu.AddDisabledItem(new GUIContent("Fetch from texture"));

            bakingContextMenu.ShowAsContext();
        }

        private void FindTextureForAllMissing()
        {
            var importers = AOTextureSearch.instance.GetAOTextureImporters();
            for (int i = 0; i < targets.Length; i++)
            {
                UnityEngine.Object target = targets[i];
                var bakeAO = target as GenericBakeAO;
                if (bakeAO.AmbientOcclusionTexture == null)
                    FindTextureForTarget(importers, target);
            }

            RefreshAllTargets();
        }

        private void FindTextureForTarget(AOTextureSearch.AOTextureImporter[] importers, UnityEngine.Object target)
        {
            var meshFilter = (target as GenericBakeAO).GetComponent<MeshFilter>();
            if (meshFilter == null)
                return;

            var mesh = meshFilter.sharedMesh;
            if (mesh == null)
                return;

            if (AOTextureSearch.instance.TryFindAOTexture(importers, mesh, out Texture2D aoTexture, out SerializableBakingSetup bakingSetup))
            {
                SerializedObject serializedTarget = new SerializedObject(target, this);
                var textureProperty = serializedTarget.FindProperty("ambientOcclusionTexture");
                if (textureProperty != null)
                    textureProperty.objectReferenceValue = aoTexture;
                var uvSetProperty = serializedTarget.FindProperty("occlusionUVSet");
                if (uvSetProperty != null)
                    uvSetProperty.enumValueIndex = (int)bakingSetup.originalMeshes[0].uv;
                serializedTarget.ApplyModifiedProperties();
                serializedTarget.Update();
            }
        }

        private void DrawMultipleObjectBakingEditor() 
        {
            DrawBakingSetupSettings();

            if (targets.Any(t => !IsComponentValid(t as GenericBakeAO)))
            {
                EditorGUILayout.HelpBox("For one or more components ambient occlusion can't be baked because of MeshFilter or SkinnedMeshRenderer components are missing.", MessageType.Warning, true);
            }

            EditorGUI.BeginDisabledGroup(!canBake);

            EditorGUILayout.Space();
            GUIRectLayout rectLayout = new GUIRectLayout(EditorGUILayout.GetControlRect());

            EditorGUI.indentLevel--;
            EditorGUI.PropertyField(rectLayout.FromRight(BakePostprocessButtonSize), bakingSetupEditor.textureAssetPostprocessActionProperty, GUIContent.none);
            EditorGUI.PropertyField(rectLayout.FromRight(MultiBakeOptionSize), bakingSetupEditor.multiTargetBakingOptionProperty, GUIContent.none);
            EditorGUI.indentLevel++;

            if (genericBakingSetup.multiTargetBakingOption == GenericBakingSetup.MultiComponentBakingOption.ForAllMissing && genericBakingSetup.textureAssetPostprocessAction == GenericBakingSetup.TextureAssetPostprocessAction.OverrideAttached)
                genericBakingSetup.textureAssetPostprocessAction = GenericBakingSetup.TextureAssetPostprocessAction.NewTexture;

            if (GUI.Button(rectLayout.GetReminder(), "Bake AO"))
            {
                BakeAOSessionSettings.instance.LastUsedBakingSetup = genericBakingSetup.Clone();
                 
                int bakedCount = 0;

                for (int i = 0; i < targets.Length; i++)
                {
                    var component = targets[i] as GenericBakeAO;
                    bool shouldBake = genericBakingSetup.multiTargetBakingOption switch
                    {
                        GenericBakingSetup.MultiComponentBakingOption.ForAll => true,
                        GenericBakingSetup.MultiComponentBakingOption.ForAllMissing => component.AmbientOcclusionTexture == null,
                        _ => true
                    };

                    if (shouldBake)
                    {
                        // TODO: texture override here
                        BakeTextureForObject(targets[i] as GenericBakeAO);
                        bakedCount++;
                    }
                }

                // TODO: Can display message if all components have assigned AO texture if (bakedCount == 0)
            }
        }

        private void DrawBakingSetupSettings()
        {
            bakingSetupEditor.OnInspectorGUI();

            // Display some warnings when users use some crazy parameters.
            if ((int)genericBakingSetup.quality.MsaaSamples > 4 && (int)genericBakingSetup.quality.TextureSize > 1000)
            {
                EditorGUILayout.HelpBox("Consider using lower Msaa Samples when you use a high Texture Size. In most cases, Msaa Samples set to 4 is enough. Using higher Msaa Samples than 4 rarely increases texture quality but greatly increases baking time and memory consumption during baking.", MessageType.Warning, true);
            }

            if (targets.Length == 1)
            {
                Bounds rendererBounds = (target as MonoBehaviour).GetComponent<Renderer>().bounds;
                float maxSize = Mathf.Max(rendererBounds.size.x, Mathf.Max(rendererBounds.size.y, rendererBounds.size.z));
                if (genericBakingSetup.quality.MaxOccluderDistance > maxSize * 0.7f)
                {
                    EditorGUILayout.HelpBox("Usually, using a Max Occluder Distance larger than the model itself will just darken the object. Consider lowering the occluder distance. Values from 0.1 to 10 typically work well. Large Occluder Distances combined with enabled baking in scene context can result in very high baking times.", MessageType.Warning, true);
                }
            }
        }

        private void BakeTextureForObject(GenericBakeAO bakeAO, string forcedTexturePath = null, Action afterBake = null)
        {
            BakeAOSessionSettings.instance.LastUsedBakingSetup = genericBakingSetup.Clone();
            genericBakingSetup.bakeAOComponentOption = GenericBakingSetup.BakeAOTexturePostprocessAction.AssignTexture;
            genericBakingSetup.TryStartBaking(bakeAO, afterBake);
        }

        #endregion

        private void UndoRedoPerformed()
        {
            RefreshAllTargets();
        }

        private void RefreshAllTargets()
        {
            foreach (var target in targets)
                (target as GenericBakeAO).Refresh();
        }
    }
}