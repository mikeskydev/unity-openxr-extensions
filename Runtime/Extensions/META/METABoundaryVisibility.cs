// SPDX-FileCopyrightText: 2024 Michael Nisbet <me@mikesky.dev>
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.InteropServices;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.NativeTypes;
using UnityEngine;
using System.IO;
using System.Xml;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
#endif

#if UNITY_EDITOR && UNITY_ANDROID
using UnityEditor.Android;
#endif

namespace OpenXR.Extensions
{
#if UNITY_EDITOR
    [OpenXRFeature(UiName = "META Boundary Visibility",
        BuildTargetGroups = new [] { BuildTargetGroup.Standalone, BuildTargetGroup.WSA, BuildTargetGroup.Android },
        Company = "Mikesky",
        Desc = "Implement XR_META_boundary_visibility",
        OpenxrExtensionStrings = XR_META_boundary_visibility,
        Version = "0.1.0",
        FeatureId = FeatureId)]
#endif
    public class METABoundaryVisibility : FeatureBase<METABoundaryVisibility>
#if UNITY_EDITOR && UNITY_ANDROID
    , IPostGenerateGradleAndroidProject
#endif
    {
        public const string XR_META_boundary_visibility = "XR_META_boundary_visibility";
        public const string FeatureId = "dev.mikesky.openxr.extensions.metaboundaryvisibility";

        public static bool SetBoundaryVisibility(bool shouldBoundaryVisibilityBeSuppressed)
        {
            XrBoundaryVisibilityMETA boundaryVisibility = shouldBoundaryVisibilityBeSuppressed ?
                XrBoundaryVisibilityMETA.XR_BOUNDARY_VISIBILITY_SUPPRESSED_META :
                XrBoundaryVisibilityMETA.XR_BOUNDARY_VISIBILITY_NOT_SUPPRESSED_META;

            var result = xrRequestBoundaryVisibilityMETA(XrSession, boundaryVisibility);
            return result == XrResult.Success;
        }

        public int callbackOrder => 0;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            if (!enabled) return;

            string manifestFolder = Path.Combine(path, "src/main");
            string file = manifestFolder + "/AndroidManifest.xml";

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(file);

                XmlElement element = (XmlElement)doc.SelectSingleNode("/manifest");
                var androidNamespaceURI = element.GetAttribute("xmlns:android");

                AndroidManifestHelper.AddOrRemoveTag(doc,
                        androidNamespaceURI,
                        "/manifest",
                        "uses-permission",
                        "com.oculus.permission.BOUNDARY_VISIBILITY",
                        true,
                        true
                );

                doc.Save(file);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        #region OpenXR native bindings
        static del_xrRequestBoundaryVisibilityMETA xrRequestBoundaryVisibilityMETA;

        protected override bool LoadBindings()
        {
            try
            {
                HookFunction("xrRequestBoundaryVisibilityMETA", ref xrRequestBoundaryVisibilityMETA);
            }
            catch (GetInstanceProcAddrException e)
            {
                Debug.LogWarning(e.Message);
                return false;
            }
            return
                xrRequestBoundaryVisibilityMETA != null &&
                true;
        }

        protected override bool CheckRequiredExtensions()
        {
            return OpenXRRuntime.IsExtensionEnabled(XR_META_boundary_visibility);
        }
        #endregion
    }
}

namespace UnityEngine.XR.OpenXR.NativeTypes
{
    public enum XrBoundaryVisibilityMETA
    {
        XR_BOUNDARY_VISIBILITY_NOT_SUPPRESSED_META = 1,
        XR_BOUNDARY_VISIBILITY_SUPPRESSED_META = 2,
        XR_BOUNDARY_VISIBILITY_MAX_ENUM_META = 0x7FFFFFFF
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct XrSystemBoundaryVisibilityPropertiesMETA
    {
        public XrStructureType type;
        public void* next;
        public bool supportsBoundaryVisibility;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct XrEventDataBoundaryVisibilityChangedMETA
    {
        public XrStructureType type;
        public void* next;
        public XrBoundaryVisibilityMETA boundaryVisibility;
    }

    delegate XrResult del_xrRequestBoundaryVisibilityMETA(ulong session, XrBoundaryVisibilityMETA boundaryVisibility);
}
