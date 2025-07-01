/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels.com/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/
﻿/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ProceduralPixels.BakeAO.Editor
{
    [System.Serializable]
    internal class SerializableBakingSetup : ISerializableBakingSetup
    {
        public uint version = 2;
        public BakingQuality quality;
        public List<SerializableMeshContext> originalMeshes; // Setup of the original baked meshes
        public List<SerializableMeshContext> meshesToBake; // Target texture will be baked for set of those meshes, this is usually just an original mesh, but it can be a different mesh, ex. processed original mesh for baking purposes.
        public List<SerializableMeshContext> occluders; // Occluders that affect the results.

        public uint Version => version;

        public SerializableBakingSetup() { }

        public SerializableBakingSetup(BakingSetup bakingSetup) : this(bakingSetup.quality, bakingSetup.originalMeshes, bakingSetup.meshesToBake, bakingSetup.occluders)
        { }

        public SerializableBakingSetup(BakingQuality quality, List<MeshContext> originalMeshes, List<MeshContext> meshesToBake, List<MeshContext> occluders)
        {
            this.quality = quality;
            this.originalMeshes = originalMeshes.Select(context => new SerializableMeshContext(context)).ToList();
            this.meshesToBake = meshesToBake.Select(context => new SerializableMeshContext(context)).ToList();
            this.occluders = occluders.Select(context => new SerializableMeshContext(context)).ToList();
        }

        public BakingSetup GetBakingSetup()
        {
            BakingSetup bakingSetup = new BakingSetup();
            bakingSetup.quality = quality;
            bakingSetup.originalMeshes = originalMeshes.Select(context => context.GetMeshContext()).ToList();
            bakingSetup.meshesToBake = meshesToBake.Select(context => context.GetMeshContext()).ToList();
            bakingSetup.occluders = occluders.Select(context => context.GetMeshContext()).ToList();

            return bakingSetup;
        }
    }
}