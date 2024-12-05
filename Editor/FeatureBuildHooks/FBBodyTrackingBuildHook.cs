using System;
using System.Collections.Generic;
using Unity.XR.Management.AndroidManifest.Editor;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR;

namespace OpenXR.Extensions.Editor
{
    public class FBBodyTrackingBuildHook : OpenXRFeatureBuildHooks
    {
        public override int callbackOrder => 0;

        public override Type featureType => typeof(FBBodyTracking);

        private static bool? FeatureRequired()
        {
            var androidOpenXRSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            var fbtrackFeature = androidOpenXRSettings.GetFeature<FBBodyTracking>();

            if (fbtrackFeature == null || !fbtrackFeature.enabled)
                return null;

            return fbtrackFeature.RequiredFeature;
        }


        protected override ManifestRequirement ProvideManifestRequirementExt()
        {
            var elementsToAdd = new List<ManifestElement>();

            if (FeatureRequired() != null)
            {
                elementsToAdd.Add(new ManifestElement()
                {
                    ElementPath = new List<string> { "manifest", "uses-feature" },
                    Attributes = new Dictionary<string, string>
                    {
                        { "name", "com.oculus.software.body_tracking" },
                        { "required", FeatureRequired().Value ? "true" : "false" }
                    }
                });
            }

            return new ManifestRequirement
            {
                SupportedXRLoaders = new HashSet<Type>()
                {
                    typeof(OpenXRLoader)
                },
                NewElements = elementsToAdd
            };
        }

        protected override void OnPostprocessBuildExt(BuildReport report)
        {
        }

        protected override void OnPreprocessBuildExt(BuildReport report)
        {
        }

        protected override void OnPostGenerateGradleAndroidProjectExt(string path)
        {
        }
    }
}
