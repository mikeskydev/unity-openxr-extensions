// SPDX-FileCopyrightText: 2024 Michael Nisbet <me@mikesky.dev>
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.InteropServices;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR;


#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
#endif

namespace OpenXR.Extensions
{
#if UNITY_EDITOR
    [OpenXRFeature(UiName = "FB Composition Layer Depth Test",
        BuildTargetGroups = new [] {BuildTargetGroup.Standalone, BuildTargetGroup.WSA, BuildTargetGroup.Android},
        Company = "Mikesky",
        Desc = "Implement XR_FB_composition_layer_depth_test",
        OpenxrExtensionStrings = "XR_FB_composition_layer_depth_test",
        Version = "0.1.0",
        FeatureId = FeatureId)]
#endif
    public class FBCompositionLayerDepthTest : FeatureBase<FBCompositionLayerDepthTest>
    {
        public const string XR_FB_composition_layer_depth_test = "XR_FB_composition_layer_depth_test";
        public const string FeatureId = "dev.mikesky.openxr.extensions.fbcompositionlayeralphablend";

        protected override bool CheckRequiredExtensions()
        {
            return OpenXRRuntime.IsExtensionEnabled(XR_FB_composition_layer_depth_test);
        }
    }
}

namespace UnityEngine.XR.OpenXR.NativeTypes
{
    public enum XrCompareOpFB
    {
        XR_COMPARE_OP_NEVER_FB = 0,
        XR_COMPARE_OP_LESS_FB = 1,
        XR_COMPARE_OP_EQUAL_FB = 2,
        XR_COMPARE_OP_LESS_OR_EQUAL_FB = 3,
        XR_COMPARE_OP_GREATER_FB = 4,
        XR_COMPARE_OP_NOT_EQUAL_FB = 5,
        XR_COMPARE_OP_GREATER_OR_EQUAL_FB = 6,
        XR_COMPARE_OP_ALWAYS_FB = 7,
        XR_COMPARE_OP_MAX_ENUM_FB = 0x7FFFFFFF
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct XrCompositionLayerDepthTestFB
    {
        public XrStructureType type;
        public void* next;
        public XrBool32 depthMask;
        public XrCompareOpFB compareOp;

        public XrCompositionLayerDepthTestFB(XrBool32 depthMask, XrCompareOpFB compareOp)
        {
            type = XrStructureTypeExt.XR_TYPE_COMPOSITION_LAYER_DEPTH_TEST_FB;
            next = null;
            this.depthMask = depthMask;
            this.compareOp = compareOp;
        }
    }
}
