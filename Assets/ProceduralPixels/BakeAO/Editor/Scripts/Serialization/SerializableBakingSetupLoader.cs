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

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ProceduralPixels.BakeAO.Editor
{
    /*
     * Follow those steps to update the SerializableBakingSetup:
     * 1. Clone existing SerializableBakingSetup.cs and rename the cloned file name and class name inside to SerializableBakingSetup_<its_version>.
     * 2. Modify SerializableBakingSetup according to your needs
     * 3. Increment version in SerializableBakingSetup and TargetVersion in this file
     * 4. Implement the function that will update previous version of SerializableBakingSetup to the newer version.
     * 5. Update loaders and updaters to handle the migration from old to new version.
     */

    /// <summary>
    /// It makes sure that serialized and deserialized data correctly updates between BakeAO versions.
    /// The logic is that when Serialized data changes, all the classes are kept in the project, and updaters are implemented using this class.
    /// SerializableBakingSetup should always be loaded using this class.
    /// </summary>
    internal class SerializableBakingSetupLoader
    {
        public const uint TargetVersion = 2;
        public static Dictionary<int, Func<ISerializableBakingSetup, ISerializableBakingSetup>> updaters; // key = source version, value = updater that increment this version during update
        public static Dictionary<int, Func<string, ISerializableBakingSetup>> loaders;

        static SerializableBakingSetupLoader()
        {
            InitializeUpdaters();
        }

        [InitializeOnLoadMethod]
        private static void InitializeUpdaters()
        {
            updaters = new();
            updaters.Add(1, From1To2);

            loaders = new();
            loaders.Add(1, SerializableBakingSetup_v1.FromJson);
            loaders.Add(2, JsonUtility.FromJson<SerializableBakingSetup>);
        }
        
        public static string ToJson(ISerializableBakingSetup bakingSetup)
        {
            SerializableBakingSetup setup = Update(bakingSetup) as SerializableBakingSetup;
            return JsonUtility.ToJson(setup, true);
        }

        /// <summary>
        /// Always updates to the newest version.
        /// </summary>
        public static SerializableBakingSetup Update(ISerializableBakingSetup setup)
        {
            if (setup.Version > TargetVersion)
                throw new InvalidOperationException("Currently serialized baking setup is from higher Bake AO version. Please update Bake AO to the newest version to use the texture.");

            while (setup.Version < TargetVersion)
                setup = updaters[(int)setup.Version].Invoke(setup);

            return setup as SerializableBakingSetup; 
        }

        public static SerializableBakingSetup FromJson(string json)
        {
            var versionObject = JsonUtility.FromJson<SerializableBakingSetupVersion>(json);
            ISerializableBakingSetup setup = loaders[(int)versionObject.version].Invoke(json);
            return Update(setup);
        }


        // ----------------------------------------------------------------------------------------
        // -----------------------------  UPDATE METHODS  -----------------------------------------
        // ----------------------------------------------------------------------------------------
        public static SerializableBakingSetup From1To2(ISerializableBakingSetup setup)
        {
            SerializableBakingSetup_v1 setupV1 = setup as SerializableBakingSetup_v1;

            SerializableBakingSetup setupV2 = new SerializableBakingSetup();
            setupV2.quality = setupV1.quality;

            // Set submeshflags to -1, because previous version didn't have this property, and always all submeshes were baked
            setupV2.originalMeshes = setupV1.meshesToBake.Select(m => new SerializableMeshContext(m) { submeshFlags = -1 }).ToList(); // Just clone meshesToBake. 
            setupV2.meshesToBake = setupV1.meshesToBake.Select(m => new SerializableMeshContext(m) { submeshFlags = -1 }).ToList();
            setupV2.occluders = setupV1.occluders.Select(m => new SerializableMeshContext(m) { submeshFlags = -1 }).ToList();

            return setupV2;
        }
    }


}