/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels.com/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace ProceduralPixels.BakeAO.Editor
{
    [System.Serializable]
    internal class BakingSetup : IProvideOccluders, IProvideMeshesToBake, IProvideBakingQuality
    {
        public BakingQuality quality;
        public List<MeshContext> originalMeshes; // Setup of the original mesh
        public List<MeshContext> meshesToBake; // Target texture will be baked for set of those meshes, this is usually just an original mesh, but it can be a different mesh, ex. processed original mesh for baking purposes.
        public List<MeshContext> occluders; // Occluders that affect the results.

        public BakingQuality BakingQuality => quality;
        public IReadOnlyList<MeshContext> Occluders => occluders;
        public IReadOnlyList<MeshContext> MeshesToBake => meshesToBake;

        public BakingSetup Copy()
        {
            BakingSetup copy = new BakingSetup();
            copy.quality = quality;
            copy.originalMeshes = originalMeshes.ToList();
            copy.meshesToBake = meshesToBake.ToList();
            copy.occluders = occluders.ToList();
            return copy;
        }

        public static BakingSetup Default => new BakingSetup()
        {
            quality = BakingQuality.Default,
            originalMeshes = new List<MeshContext>(),
            meshesToBake = new List<MeshContext>(),
            occluders = new List<MeshContext>(),
        };
    }
}