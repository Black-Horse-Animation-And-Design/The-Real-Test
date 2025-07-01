/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels.com/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ProceduralPixels.BakeAO.Editor
{
    // Version from BakeAO 1.0.1
    [System.Serializable]
    internal class SerializableBakingSetup_v1 : ISerializableBakingSetup
    {
        public uint version = 1;
        public BakingQuality quality;
        public List<SerializableMeshContext> meshesToBake;
        public List<SerializableMeshContext> occluders;

        public uint Version => version;

        public SerializableBakingSetup_v1(BakingSetup bakingSetup) : this(bakingSetup.quality, bakingSetup.meshesToBake, bakingSetup.occluders)
        { }

        public SerializableBakingSetup_v1(BakingQuality quality, List<MeshContext> meshesToBake, List<MeshContext> occluders)
        {
            version = 1;
            this.quality = quality;
            this.meshesToBake = meshesToBake.Select(context => new SerializableMeshContext(context)).ToList();
            this.occluders = occluders.Select(context => new SerializableMeshContext(context)).ToList();
        }

        public BakingSetup GetBakingSetup()
        {
            BakingSetup bakingSetup = new BakingSetup();
            bakingSetup.quality = quality;
            bakingSetup.meshesToBake = meshesToBake.Select(context => context.GetMeshContext()).ToList();
            bakingSetup.originalMeshes = meshesToBake.Select(context => context.GetMeshContext()).ToList();
            bakingSetup.occluders = occluders.Select(context => context.GetMeshContext()).ToList();

            return bakingSetup;
        }

        public string ToJson(bool prettyPrint)
        {
            return JsonUtility.ToJson(this, prettyPrint);
        }

        public static SerializableBakingSetup_v1 FromJson(string json)
        {
            return JsonUtility.FromJson<SerializableBakingSetup_v1>(json);
        }
    }
}