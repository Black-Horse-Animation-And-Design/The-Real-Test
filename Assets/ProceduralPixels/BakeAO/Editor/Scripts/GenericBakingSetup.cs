/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels.com/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using static ProceduralPixels.BakeAO.Editor.BakeAOUtils;

namespace ProceduralPixels.BakeAO.Editor
{
    [CreateAssetMenu(menuName = "Procedural Pixels/Bake AO/Generic Baking Setup", fileName = "Bake AO Setup")]
    internal class GenericBakingSetup : ScriptableObject
    {
        [Tooltip("Source UV channel for baked texture.")]
        public UVChannel uvChannel;

        [Tooltip("Which submeshes of a mesh should be used for baking.")]
        public long targetSubmeshFlags = -1;

        [Tooltip("Which submeshes from the target mesh should be used as occluders.")]
        public long occluderSubmeshFlags = -1;

        [Tooltip("Baking quality settings. Defines quality and resolution of the baked texture.")]
        public BakingQuality quality;

        [Tooltip("How nearby objects should affect the baking.")]
        public ContextBakingSettings contextBakingSettings;

        [Tooltip("Where to save the baked texture. Allows to input parameters like <MeshFolder>, <MeshName>, <GameObjectName> and <AssetName>. Example: <MeshFolder>/AOTextures/<MeshName>_AO.png")]
        public string filePath = "<MeshFolder>/AOTextures/<MeshName>_AO.png";

        [Tooltip("If file path uses <MeshFolder> parameter, and mesh is not saved in the project folder (is built-in or from packages) this folder will be used instead")]
        public string meshFolderFallback = "Assets/BakeAO/Textures";

        [Tooltip("Defines how the baked texture should be saved.")]
        public TextureAssetPostprocessAction textureAssetPostprocessAction = TextureAssetPostprocessAction.NewTexture;

        [Tooltip("Defines for what objects the texture should be baked.")]
        public MultiComponentBakingOption multiTargetBakingOption = MultiComponentBakingOption.ForAll;

        [Tooltip("Defines what to do with Bake AO component after baking is finished.")]
        public BakeAOTexturePostprocessAction bakeAOComponentOption = BakeAOTexturePostprocessAction.None;

        public static GenericBakingSetup Default
        {
            get
            {
                var setup = ScriptableObject.CreateInstance<GenericBakingSetup>();

                setup.uvChannel = UVChannel.UV0;
                setup.targetSubmeshFlags = -1;
                setup.occluderSubmeshFlags = -1;
                setup.quality = BakingQuality.Default;
                setup.contextBakingSettings = new ContextBakingSettings();
                setup.filePath = "<MeshFolder>/AOTextures/<MeshName>_AO.png";
                setup.meshFolderFallback = "Assets/BakeAO/Textures";
                setup.textureAssetPostprocessAction = TextureAssetPostprocessAction.NewTexture;
                setup.multiTargetBakingOption = MultiComponentBakingOption.ForAll;
                setup.bakeAOComponentOption = BakeAOTexturePostprocessAction.None;

                return setup; 
            }
        } 

        public static GenericBakingSetup DefaultFromPresets
        {
            get
            {
                var setup = Default;
                var presets = UnityEditor.Presets.Preset.GetDefaultPresetsForObject(setup);
                foreach (var preset in presets)
                    preset.ApplyTo(setup);

                return setup;
            }
        }

        public GenericBakingSetup Clone()
        {
            var setup = CreateInstance<GenericBakingSetup>();
            setup.hideFlags = HideFlags.DontSave;

            setup.uvChannel = uvChannel;
            setup.targetSubmeshFlags = targetSubmeshFlags;
            setup.occluderSubmeshFlags = occluderSubmeshFlags;
            setup.quality = quality;
            setup.contextBakingSettings = contextBakingSettings;
            setup.filePath = filePath;
            setup.meshFolderFallback = meshFolderFallback;
            setup.bakeAOComponentOption = bakeAOComponentOption;
            setup.textureAssetPostprocessAction = textureAssetPostprocessAction;
            setup.multiTargetBakingOption = multiTargetBakingOption;

            return setup;
        }

        public bool TryGetBakingSetup(UnityEngine.Object unityObject, out BakingSetup bakingSetup)
        {
            bakingSetup = null;
            bool isSuccess = false;

            if (unityObject is Mesh mesh)
                isSuccess = BakeAOUtils.TryGetBakingSetup(mesh, targetSubmeshFlags, occluderSubmeshFlags, uvChannel, quality, out bakingSetup);
            else
            {
                if (unityObject is GameObject go)
                    unityObject = go.GetComponent<Component>();

                if (unityObject is Component component)
                {
                    MeshFilter meshFilter = component.GetComponent<MeshFilter>();
                    SkinnedMeshRenderer skinnedMeshRenderer = component.GetComponent<SkinnedMeshRenderer>();

                    if (meshFilter != null)
                        isSuccess = BakeAOUtils.TryGetBakingSetup(meshFilter, targetSubmeshFlags, occluderSubmeshFlags, uvChannel, quality, contextBakingSettings, out bakingSetup);
                    else if (skinnedMeshRenderer != null)
                        isSuccess = BakeAOUtils.TryGetBakingSetup(skinnedMeshRenderer, targetSubmeshFlags, occluderSubmeshFlags, uvChannel, quality, contextBakingSettings, out bakingSetup);
                }
            }

            if (!isSuccess)
                return false;

            LODGroup lodGroup = BakeAOUtils.FirstOrDefaultLODGroup(unityObject);

            if (lodGroup == null)
                return true;

            Mesh meshToBake = bakingSetup.originalMeshes[0].mesh;
            LOD[] lods = lodGroup.GetLODs();
            bool containsMeshToBake = lods.Any(lod => lod.renderers.Any(r => BakeAOUtils.FirstOrDefaultMesh(r) == meshToBake));
            if (containsMeshToBake)
            {
                var allLodMeshes = lods.SelectMany(lod => lod.renderers.Select(r => BakeAOUtils.FirstOrDefaultMesh(r))).Where(m => m != null && m != meshToBake);
                bakingSetup.occluders.RemoveAll(m => allLodMeshes.Contains(m.mesh));
            }

            return true;
        }

        public bool TryStartBaking(UnityEngine.Object sourceObject, Action afterBake)
        {
            Validate();

            // Check if the sourceObject is valid Mesh or Component
            Mesh meshToBake;
            Component sourceComponent = null;
            bool isSourceObjectMesh = sourceObject is Mesh;
            bool isSourceObjectComponent = sourceObject is Component;

            if (isSourceObjectComponent)
            {
                sourceComponent = sourceObject as Component;

                UnityEngine.Object objectToBake = sourceComponent.GetComponent<MeshFilter>();
                if (objectToBake == null)
                    objectToBake = sourceComponent.GetComponent<SkinnedMeshRenderer>();

                if (objectToBake == null)
                {
                    BakeAOErrorMeshList.AddError(new BakeAOErrorMeshList.ErrorData("Can't find associated mesh", sourceComponent));
                    return false;
                }
            }

            if (BakeAOUtils.TryGetMesh(sourceObject, out meshToBake))
            {
                if (!meshToBake.DoesHaveUVSet(uvChannel))
                {
                    BakeAOErrorMeshList.AddError(new BakeAOErrorMeshList.ErrorData($"Missing uv set: {uvChannel}", meshToBake));
                    return false;
                }

                if (targetSubmeshFlags == 0)
                {
                    BakeAOErrorMeshList.AddError(new BakeAOErrorMeshList.ErrorData($"No submesh was set for baking.", meshToBake));
                    return false;
                }
            }

            if (TryGetBakingSetup(sourceObject, out BakingSetup bakingSetup))
            {
                var sourceMeshContext = bakingSetup.originalMeshes[0];
                var sourceMesh = sourceMeshContext.mesh;

                // Test to see if baking of submeshes works correctly.
                //sourceMeshContext.submeshFlags = 7;
                //sourceMeshContext.useFlags |= MeshContextUseFlags.DontCombine;
                //bakingSetup.meshesToBake[0] = sourceMeshContext;

                //var occluder = bakingSetup.occluders[0];
                //occluder.submeshFlags = 7;
                //occluder.useFlags |= MeshContextUseFlags.DontCombine;
                //bakingSetup.occluders[0] = occluder;
                // End of test

                // Test baking of SkinnedMeshRenderer mesh.
                //GameObject go = new GameObject("SMRTest", typeof(MeshFilter), typeof(MeshRenderer));
                //go.GetComponent<MeshFilter>().sharedMesh = bakingSetup.meshesToBake[0].mesh;
                //go.GetComponent<Renderer>().sharedMaterials = (objectToBake as Component).GetComponent<Renderer>().sharedMaterials;
                //go.transform.position = bakingSetup.meshesToBake[0].objectToWorld.GetPosition();
                //go.transform.rotation = bakingSetup.meshesToBake[0].objectToWorld.rotation;

                string targetFilePath = null;
                GenericBakeAO bakeAOComponent;
                bool forcePath = false; // True if texture needs to be forced to be saved into given path.

                TryGetBakeAOComponent(sourceObject, out bakeAOComponent);

                if (bakeAOComponentOption == BakeAOTexturePostprocessAction.CreateAndAssignTexture)
                {
                    if (bakeAOComponent == null)
                        TryGetOrCreateBakeAOComponent(sourceObject, out bakeAOComponent);
                }

                if (textureAssetPostprocessAction == TextureAssetPostprocessAction.OverrideAttached)
                {
                    if (bakeAOComponent != null)
                    {
                        var texture = bakeAOComponent.AmbientOcclusionTexture;
                        if (texture != null && AssetDatabase.IsMainAsset(texture))
                        {
                            targetFilePath = AssetDatabase.GetAssetPath(texture);
                            forcePath = true;
                        }
                    }
                }
                else if (textureAssetPostprocessAction == TextureAssetPostprocessAction.OverrideInPath)
                    forcePath = true;

                if (targetFilePath == null)
                {
                    BakeAOUtils.PathResolver resolver = isSourceObjectMesh ? new PathResolver(sourceMesh) : new BakeAOUtils.PathResolver(sourceComponent.gameObject, sourceMesh);
                    resolver.fallbackMeshFolderPath = meshFolderFallback;
                    targetFilePath = resolver.Resolve(filePath);
                    targetFilePath = PathUtils.EnsureContainsExtension(targetFilePath, ".png");

                    PathUtils.EnsureDirectoryExists(PathUtils.GetContainingFolderPath(targetFilePath));
                }

                if (bakeAOComponentOption == BakeAOTexturePostprocessAction.None)
                    bakeAOComponent = null;

                BakeAOUtils.Bake(bakingSetup, SaveTextureForBakeAOPostprocessor.Create(targetFilePath, forcePath, bakeAOComponent, afterBake), sourceObject);
                return true;
            }

            return false;
        }

        public bool ArePathsValid()
        {
            return PathResolver.ValidateTemplate(filePath, out var _, out bool _) && PathResolver.ValidatePath(meshFolderFallback, out var _, out bool _);
        }

        public void Validate()
        {
            quality.Validate();

            if (!BakeAOPreferences.instance.showSubmeshSelectionInBakingSettings)
            {
                targetSubmeshFlags = -1;
                occluderSubmeshFlags = -1;
            }

            if (multiTargetBakingOption == GenericBakingSetup.MultiComponentBakingOption.ForAllMissing && textureAssetPostprocessAction == GenericBakingSetup.TextureAssetPostprocessAction.OverrideAttached)
                textureAssetPostprocessAction = GenericBakingSetup.TextureAssetPostprocessAction.NewTexture;
        }

        private void OnValidate()
        {
            Validate();
        }

        public enum TextureAssetPostprocessAction
        {
            NewTexture = 0,
            OverrideAttached = 1,
            OverrideInPath = 2
        }

        public enum MultiComponentBakingOption
        {
            ForAll = 0,
            ForAllMissing = 1
        }

        public enum BakeAOTexturePostprocessAction
        {
            None = 0,
            CreateAndAssignTexture = 1,
            AssignTexture = 2
        }
    }
}