using ProceduralPixels.BakeAO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProceduralPixels.BakeAO
{
    /// <summary>
    /// Stores Bake AO project-related settings that are related to the runtime.
    /// </summary>
    public class BakeAORuntimeSettings : ScriptableObject
    {
        private static BakeAORuntimeSettings instance;
        public static BakeAORuntimeSettings Instance
        {
            get
            {
#if UNITY_EDITOR
                if (instance != null)
                    return instance;
                else
                {
                    instance = GetOrCreateAsset();
                    return instance;
                }
#else
            return instance;
#endif
            }
        }

        [SerializeField]
        private BakeAOApplyMode defaultApplyMode = BakeAOApplyMode.MaterialInstance;

        public BakeAOApplyMode applyMode => pickedApplyMode;

        private BakeAOApplyMode pickedApplyMode = BakeAOApplyMode.Default;

        private void OnEnable()
        {
            if (instance == null)
                instance = this;

            RefreshApplyMode();
        }

        /// <summary>
        /// Bake AO settings cache the Apply Mode, so if you want to read from applyMode in runtime, you need to call RefreshApplyMode at least once, if it changed during runtime.
        /// </summary>
        public void RefreshApplyMode()
        {
            pickedApplyMode = defaultApplyMode;
            if (pickedApplyMode == BakeAOApplyMode.Default)
            {
                if (GraphicsSettings.defaultRenderPipeline == null)
                    pickedApplyMode = BakeAOApplyMode.MaterialPropertyBlock;
                else
                    pickedApplyMode = BakeAOApplyMode.MaterialInstance;
            }
        }

        private void OnDisable()
        {
            if (instance == this)
                instance = null;
        }

#if UNITY_EDITOR
        private const string TargetFolderGUID = "d34bf71e967be164c823eac777943a01"; // Settings folder
        private const string AlternativeFolderGUID = "10884a463c5bea14fa71246adb0b45b9"; // Bake AO folder
        private const string DefaultSettingsAssetName = nameof(BakeAORuntimeSettings) + ".asset";

        private static string GetTargetFolderPath()
        {
            string targetFolderPath = AssetDatabase.GUIDToAssetPath(TargetFolderGUID);

            if (string.IsNullOrWhiteSpace(targetFolderPath))
                targetFolderPath = AssetDatabase.GUIDToAssetPath(AlternativeFolderGUID);

            if (string.IsNullOrWhiteSpace(targetFolderPath))
                targetFolderPath = "Assets" + System.IO.Path.PathSeparator;

            return targetFolderPath;
        }

        private static string GetTargetAssetPath()
        {
            return System.IO.Path.Combine(GetTargetFolderPath(), DefaultSettingsAssetName);
        }

        public static BakeAORuntimeSettings GetOrCreateAsset()
        {
            var guid = UnityEditor.AssetDatabase.FindAssets($"t:{nameof(BakeAORuntimeSettings)}").FirstOrDefault();
            if (guid == null)
            {
                var newSettings = CreateInstance<BakeAORuntimeSettings>();
                var targetPath = GetTargetAssetPath();
                AssetDatabase.CreateAsset(newSettings, targetPath);

                Debug.Log($"New Bake AO Runtime Settings asset was created at path: {targetPath}", newSettings);

                return newSettings;
            }

            var path = AssetDatabase.GUIDToAssetPath(guid);
            var settings = AssetDatabase.LoadAssetAtPath<BakeAORuntimeSettings>(path);

            return settings;
        }
#endif
    }
}