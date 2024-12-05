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
    public class METABoundaryVisibilityBuildHook : OpenXRFeatureBuildHooks
    {
        public override int callbackOrder => 0;

        public override Type featureType => typeof(METABoundaryVisibility);

        protected override ManifestRequirement ProvideManifestRequirementExt()
        {
            return new ManifestRequirement
            {
                SupportedXRLoaders = new HashSet<Type>()
                {
                    typeof(OpenXRLoader)
                },
                NewElements = new List<ManifestElement>()
                {
                    new ManifestElement()
                    {
                        ElementPath = new List<string> { "manifest", "uses-permission" },
                        Attributes = new Dictionary<string, string>
                        {
                            { "name", "com.oculus.permission.BOUNDARY_VISIBILITY" }
                        }
                    },
                }
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
