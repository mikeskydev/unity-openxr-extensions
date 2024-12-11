// SPDX-FileCopyrightText: 2024 Michael Nisbet <me@mikesky.dev>
// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.NativeTypes;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
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
    {
        public const string XR_META_boundary_visibility = "XR_META_boundary_visibility";
        public const string FeatureId = "dev.mikesky.openxr.extensions.metaboundaryvisibility";

        public static bool SuppressBoundaryVisibility(bool shouldBoundaryVisibilityBeSuppressed)
        {
            XrBoundaryVisibilityMETA boundaryVisibility = shouldBoundaryVisibilityBeSuppressed ?
                XrBoundaryVisibilityMETA.XR_BOUNDARY_VISIBILITY_SUPPRESSED_META :
                XrBoundaryVisibilityMETA.XR_BOUNDARY_VISIBILITY_NOT_SUPPRESSED_META;

            var result = xrRequestBoundaryVisibilityMETA(XrSession, boundaryVisibility);
            return result == XrResult.Success;
        }

        #region OpenXR native bindings
        static del_xrRequestBoundaryVisibilityMETA xrRequestBoundaryVisibilityMETA;

        protected override bool HookFunctions()
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

        protected override void UnhookFunctions()
        {
            xrRequestBoundaryVisibilityMETA = null;
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
