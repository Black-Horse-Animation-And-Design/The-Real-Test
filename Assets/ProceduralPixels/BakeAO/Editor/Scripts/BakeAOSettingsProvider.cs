/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels.com/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

using System.Collections.Generic;
using UnityEditor;
using System.Linq;
using UnityEngine.UIElements;

namespace ProceduralPixels.BakeAO.Editor
{
    class BakeAOSettingsProvider : SettingsProvider
    {
        UnityEditor.Editor settingsEditor;
        UnityEditor.Editor runtimeSettingsEditor;

        public BakeAOSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords)
        {
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            settingsEditor = UnityEditor.Editor.CreateEditor(BakeAOSettings.Instance);
            runtimeSettingsEditor = UnityEditor.Editor.CreateEditor(BakeAORuntimeSettings.GetOrCreateAsset());
        }

        public override void OnGUI(string searchContext)
        {
            settingsEditor.OnInspectorGUI();
            EditorGUILayout.Space(32);
            runtimeSettingsEditor.OnInspectorGUI();
        }

        [SettingsProvider]
        public static SettingsProvider CreateBakeAOSettingProvider()
        {
            IEnumerable<string> properties = 
                GetSearchKeywordsFromGUIContentProperties<BakeAOSettingsEditor.Styles>()
                .Union(GetSearchKeywordsFromGUIContentProperties<BakeAORuntimeSettingsEditor.Styles>());

            var provider = new BakeAOSettingsProvider("Project/Bake AO", SettingsScope.Project, properties);
            return provider;
        }
    }
}