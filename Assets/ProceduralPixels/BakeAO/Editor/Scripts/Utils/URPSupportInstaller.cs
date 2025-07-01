/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels.com/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace ProceduralPixels.BakeAO.Editor
{
    internal class VersionData : IComparable<VersionData>
    {
        public int major;
        public int minor;
        public int patch;

        public string supportPackageGUID;

        public VersionData(string version, string supportPackageGUID = null)
        {
            major = 0;
            minor = 0;
            patch = 0;

            var split = version.Split('.');

            if (split.Length >= 1)
                ParseNumber(split[0], ref major);

            if (split.Length >= 2)
                ParseNumber(split[1], ref minor);

            if (split.Length >= 3)
                ParseNumber(split[2], ref patch);

            this.supportPackageGUID = supportPackageGUID;
        }

        public static void ParseNumber(string str, ref int refValue)
        {
            if (int.TryParse(str, out int result))
                refValue = result;
        }

        internal long RawValue => (major * 1000 * 1000) + (minor * 1000) + patch;

        public int CompareTo(VersionData other)
        {
            return this.RawValue.CompareTo(other.RawValue);
        }
    }

    internal class URPSupportInstaller : EditorWindow
    {
        public const string URP12SupportGUID = "864111a15a126c443b731078721406fb";
        public const string URP13SupportGUID = "f62ecaeda58b742488ebe658fdb9434b";
        public const string URP14SupportGUID = "e2d4ab0a9c78be649ba4d3a01f1dcc2a";
        public const string URP15SupportGUID = "8a2f5a687e9df8348969cf2a833d9301";
        public const string URP16SupportGUID = "22b4fe3d5f67ce14abefadd801860599";
        public const string URP17SupportGUID = "bb6e35de6cebf594da7347cfd7d7a568";
        public const string URP17_1SupportGUID = "87f49d36ad119a84eaa00bd917dd1618";

        internal List<VersionData> supportedVersions = null;

        private static ListRequest listRequest;

        [SerializeField] private bool requestRestart = false;

        private void OnEnable()
        {
            supportedVersions = new List<VersionData>()
            {
                new VersionData("12.0", URP12SupportGUID),
                new VersionData("13.0", URP13SupportGUID),
                new VersionData("14.0", URP14SupportGUID),
                new VersionData("15.0", URP15SupportGUID),
                new VersionData("16.0", URP16SupportGUID),
                new VersionData("17.0", URP17SupportGUID),
                new VersionData("17.1", URP17_1SupportGUID),
            };

            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        [MenuItem("Window/Procedural Pixels/Bake AO/URP Support Installer")]
        public static void Open()
        {
            CreateWindow<URPSupportInstaller>("URP Support Installer");
        }

        private void OnGUI()
        {
            this.minSize = new Vector2(570, 250);

            if (GUILayout.Button("Open Documentation"))
                Application.OpenURL("https://proceduralpixels.com/BakeAO/Documentation/URPSupportInstaller");

            EditorGUILayout.HelpBox("This installer will help you install URP support for Bake AO in your project.", MessageType.Info, true);

            if (listRequest == null || !listRequest.IsCompleted)
            {
                CheckURPInstallation();
                EditorGUILayout.HelpBox("Checking installed URP version, please wait...", MessageType.Info);
                return;
            }

            if (listRequest.Status == StatusCode.Success)
            {
                bool isURPInstalled = false;
                bool isURPSupportInstalled = false;
                string installedURPVersion = "";
                string installedURPSupportVersion = "";

                var urpPackage = listRequest.Result.FirstOrDefault(package => package.name.Contains("com.unity.render-pipelines.universal", System.StringComparison.InvariantCultureIgnoreCase));
                isURPInstalled = urpPackage != null;
                if (isURPInstalled)
                    installedURPVersion = urpPackage.version;

                var supportVersionField = TypeCache.GetFieldsWithAttribute(typeof(BakeAOURPSupportVersionAttribute)).FirstOrDefault();
                if (supportVersionField != null)
                {
                    isURPSupportInstalled = true;
                    installedURPSupportVersion = supportVersionField.GetValue(null) as string;
                }

                // Installation

                if (isURPInstalled)
                {
                    EditorGUILayout.LabelField("URP is installed. Detected Version: " + installedURPVersion);

                    if (isURPSupportInstalled)
                    {
                        EditorGUILayout.LabelField("Bake AO URP Support is installed. Detected version: " + installedURPSupportVersion);
                        ShowURPSupportOptions(installedURPVersion, installedURPSupportVersion);
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Bake AO support for URP is not installed.");
                        if (GUILayout.Button("Install URP Support"))
                            InstallURPSupport(installedURPVersion);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("URP is not installed. Please install URP using Package Manager first.", MessageType.Warning);
                }

                // Apply mode suggestion

                EditorGUILayout.Space(32);
                var runtimeSettings = BakeAORuntimeSettings.GetOrCreateAsset();
                runtimeSettings.RefreshApplyMode();

                if (isURPInstalled && runtimeSettings.applyMode == BakeAOApplyMode.MaterialPropertyBlock)
                {
                    EditorGUILayout.HelpBox($"It is recommended to use {BakeAOApplyMode.MaterialInstance} when using URP. Considering updating the default Bake AO Apply Mode in the \"Project Settings/Bake AO\"", MessageType.Warning);

                    if (GUILayout.Button($"Change default Bake AO Apply Mode to {BakeAOApplyMode.MaterialInstance}"))
                        BakeAORuntimeSettingsEditor.SetDefaultApplyMode(BakeAORuntimeSettings.GetOrCreateAsset(), BakeAOApplyModeEditor.MaterialInstance);
                }

                if (!isURPInstalled && runtimeSettings.applyMode == BakeAOApplyMode.MaterialInstance)
                {
                    EditorGUILayout.HelpBox($"It is recommended to use {BakeAOApplyMode.MaterialPropertyBlock} when using URP. Considering updating the default Bake AO Apply Mode in the \"Project Settings/Bake AO\"\nCurrently used apply mode is", MessageType.Warning);

                    if (GUILayout.Button($"Change default Bake AO apply mode to {BakeAOApplyMode.MaterialPropertyBlock}"))
                        BakeAORuntimeSettingsEditor.SetDefaultApplyMode(BakeAORuntimeSettings.GetOrCreateAsset(), BakeAOApplyModeEditor.MaterialPropertyBlock);

                    if (GUILayout.Button($"Documentation"))
                        BakeAOUtils.OpenDocumentation();
                }
            }
            else if (listRequest.Status >= StatusCode.Failure)
            {
                EditorGUILayout.HelpBox("Failed to check installed packages: " + listRequest.Error.message, MessageType.Error);
            }
        }

        private void RestartEditor()
        {
            // Restart the editor by reopening the current project, but save the changed assets firtst
            AssetDatabase.SaveAssets();
            string projectPath = Path.GetDirectoryName(Application.dataPath);
            EditorApplication.OpenProject(projectPath);
        }

        private void CheckURPInstallation()
        {
            if (listRequest == null || listRequest.Status == StatusCode.Failure)
            {
                listRequest = Client.List(); // Initiates an asynchronous request to list all installed packages

            }
        }

        private void OnEditorUpdate()
        {
            if (listRequest != null && listRequest.IsCompleted)
            {
                Repaint();
            }

            if (requestRestart)
            {
                bool shouldRestart = EditorUtility.DisplayDialog("Restart required", "Bake AO support for URP was installed. You need to restart the editor to make Bake AO support for URP work correctly.", "Save project and restart", "Ignore");
                requestRestart = false;
                if (shouldRestart)
                    RestartEditor();
            }
        }

        private void ShowURPSupportOptions(string urpVersion, string supportVersion)
        {
            bool doesSupportExist = GetVersionData(urpVersion) != null;
            if (!doesSupportExist)
            {
                EditorGUILayout.HelpBox("Bake AO support for currently installed URP does not exist. If you think that this error should not exist, please contact me at dev@proceduralpixels.com", MessageType.Error, true);
                return;
            }

            if (GetMajorVersion(supportVersion) != GetMajorVersion(urpVersion))
            {
                EditorGUILayout.HelpBox("URP Support is not matching installed URP version. Please update.", MessageType.Warning);

                if (GUILayout.Button("Update URP Support"))
                {
                    UninstallURPSupport();
                    InstallURPSupport(urpVersion);
                }
            }
            else
            {
                if (GUILayout.Button("Uninstall URP Support"))
                {
                    UninstallURPSupport();
                    EditorUtility.DisplayDialog("Bake AO support for URP", "Bake AO support for URP was uninstalled", "Ok");
                }
            }
        }

        private void UninstallURPSupport()
        {
            var folderGUIDField = TypeCache.GetFieldsWithAttribute<BakeAOURPSupportFolderGUIDAttribute>().FirstOrDefault();
            if (folderGUIDField == null)
                Debug.LogError("No folder found for installed Bake AO URP support. It was probably manually modified.");

            var folderPath = AssetDatabase.GUIDToAssetPath(folderGUIDField.GetValue(null) as string);
            if (!string.IsNullOrEmpty(folderPath) && AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.DeleteAsset(folderPath);
                AssetDatabase.Refresh();
                UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
                listRequest = null;
            }
            else
                Debug.LogError("No folder found for installed Bake AO URP support. It was probably manually modified. To uninstall Bake AO URP Support, find the folder URPxx, where xx is the major URP version and delete this folder manually.");
        }

        private void InstallURPSupport(string urpVersion)
        {
            var closestVersion = GetVersionData(urpVersion);
            var packagePath = AssetDatabase.GUIDToAssetPath(closestVersion.supportPackageGUID);
            AssetDatabase.ImportPackage(packagePath, false);
            listRequest = null;
            requestRestart = true;
        }

        private VersionData GetVersionData(string urpVersion)
        {
            VersionData targetVersion = new VersionData(urpVersion);

            VersionData closestVersion = supportedVersions[0];
            for (int i = 1; i < supportedVersions.Count; i++)
            {
                var other = supportedVersions[i];
                if (targetVersion.RawValue < other.RawValue)
                    continue;

                if (other.RawValue > closestVersion.RawValue)
                    closestVersion = other;
            }

            if (closestVersion.major != targetVersion.major)
                return null;

            return closestVersion;
        }

        private int GetMajorVersion(string fullVersion)
        {
            return new VersionData(fullVersion, "").major;
        }
    }

    // Any const in the code with this attribute should contain installed URP support version. If no field exists, there is no URP support installed.
    public class BakeAOURPSupportVersionAttribute : System.Attribute
    { }

    // Any const in the code with this attribute should contain installed URP support folder GUID
    public class BakeAOURPSupportFolderGUIDAttribute : System.Attribute
    {

    }
}
