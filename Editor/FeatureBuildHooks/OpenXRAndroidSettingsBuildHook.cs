using System;
using System.Collections.Generic;
using System.IO;
using Unity.XR.Management.AndroidManifest.Editor;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.XR.OpenXR;

namespace OpenXR.Extensions.Editor
{
    public class OpenXRAndroidSettingsBuildHook : OpenXRFeatureBuildHooks
    {
        public override int callbackOrder => 0;

        public override Type featureType => typeof(OpenXRAndroidSettings);

        private static Texture2D SystemSplashScreen()
        {
            var androidOpenXRSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            var androidFeature = androidOpenXRSettings.GetFeature<OpenXRAndroidSettings>();

            if (androidFeature == null || !androidFeature.enabled)
                return null;

            return androidFeature.systemSplashScreen;
        }

        private static void ProcessSystemSplashScreen(string gradlePath)
        {
            var systemSplashScreen = SystemSplashScreen();
            if (systemSplashScreen == null)
                return;

            string splashScreenAssetPath = AssetDatabase.GetAssetPath(systemSplashScreen);
            string sourcePath = splashScreenAssetPath;
            string targetFolder = Path.Combine(gradlePath, "src/main/assets");
            string targetPath = targetFolder + "/vr_splash.png";

            // copy the splash over into the gradle folder and make sure it's not read only
            FileUtil.ReplaceFile(sourcePath, targetPath);
            FileInfo targetInfo = new FileInfo(targetPath);
            targetInfo.IsReadOnly = false;
        }

        protected override ManifestRequirement ProvideManifestRequirementExt()
        {
            var elementsToRemove = new List<ManifestElement>();

            var elementsToAdd = new List<ManifestElement>()
            {
                // Oculus store requirement, despite not preventing an OpenXR app running.
                new ManifestElement()
                {
                    ElementPath = new List<string> { "manifest", "uses-feature" },
                    Attributes = new Dictionary<string, string>
                    {
                        { "name", "android.hardware.vr.headtracking" },
                        { "required", "true" },
                        { "version", "1" }
                    }
                },
                // Oculus store requirement, despite not preventing an OpenXR app running.
                new ManifestElement()
                {
                    ElementPath = new List<string> { "manifest", "application", "activity", "meta-data" },
                    Attributes = new Dictionary<string, string>
                    {
                        { "name", "com.oculus.vr.focusaware" },
                        { "value", "true" }
                    }
                },
                // Oculus store requirement, despite not preventing an OpenXR app running.
                new ManifestElement()
                {
                    ElementPath = new List<string> { "manifest", "application", "activity", "intent-filter", "category" },
                    Attributes = new Dictionary<string, string>
                    {
                        { "name", "com.oculus.intent.category.VR" }
                    }
                },
            };
            
            // Oculus compositor driven splash screen
            if (SystemSplashScreen() != null)
            {
                elementsToAdd.Add(new ManifestElement()
                {
                    ElementPath = new List<string> { "manifest", "application", "meta-data" },
                    Attributes = new Dictionary<string, string>
                    {
                        { "name", "com.oculus.ossplash" },
                        { "value", "true" }
                    }
                });
            }
            
            return new ManifestRequirement
            {
                SupportedXRLoaders = new HashSet<Type>()
                {
                    typeof(OpenXRLoader)
                },
                NewElements = elementsToAdd,
                RemoveElements = elementsToRemove
            };
        }

        protected override void OnPostGenerateGradleAndroidProjectExt(string path)
        {
            ProcessSystemSplashScreen(path);
        }

        protected override void OnPostprocessBuildExt(BuildReport report)
        {
        }

        protected override void OnPreprocessBuildExt(BuildReport report)
        {
        }
    }
}
