using System;
using System.Collections.Generic;
using Unity.XR.Management.AndroidManifest.Editor;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR;

namespace OpenXR.Extensions.Editor
{
    public class FBPassthroughBuildHook : OpenXRFeatureBuildHooks
    {
        public override int callbackOrder => 0;

        public override Type featureType => typeof(FBPassthrough);

        private static bool? FeatureRequired()
        {
            var androidOpenXRSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            var fbpassFeature = androidOpenXRSettings.GetFeature<FBPassthrough>();

            if (fbpassFeature == null || !fbpassFeature.enabled)
                return null;

            return fbpassFeature.RequiredFeature;
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
                        { "name", "com.oculus.feature.PASSTHROUGH" },
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
