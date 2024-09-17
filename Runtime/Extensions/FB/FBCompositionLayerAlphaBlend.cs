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
    [OpenXRFeature(UiName = "FB Composition Layer Alpha Blend",
        BuildTargetGroups = new [] {BuildTargetGroup.Standalone, BuildTargetGroup.WSA, BuildTargetGroup.Android},
        Company = "Mikesky",
        Desc = "Implement XR_FB_composition_layer_alpha_blend",
        OpenxrExtensionStrings = "XR_FB_composition_layer_alpha_blend",
        Version = "0.1.0",
        FeatureId = FeatureId)]
#endif
    public class FBCompositionLayerAlphaBlend : FeatureBase<FBCompositionLayerAlphaBlend>
    {
        public const string XR_FB_composition_layer_alpha_blend = "XR_FB_composition_layer_alpha_blend";
        public const string FeatureId = "dev.mikesky.openxr.extensions.fbcompositionlayeralphablend";

        protected override bool CheckRequiredExtensions()
        {
            return OpenXRRuntime.IsExtensionEnabled(XR_FB_composition_layer_alpha_blend);
        }
    }
}

namespace UnityEngine.XR.OpenXR.NativeTypes
{
    public enum XrBlendFactorFB
    {
        XR_BLEND_FACTOR_ZERO_FB = 0,
        XR_BLEND_FACTOR_ONE_FB = 1,
        XR_BLEND_FACTOR_SRC_ALPHA_FB = 2,
        XR_BLEND_FACTOR_ONE_MINUS_SRC_ALPHA_FB = 3,
        XR_BLEND_FACTOR_DST_ALPHA_FB = 4,
        XR_BLEND_FACTOR_ONE_MINUS_DST_ALPHA_FB = 5,
        XR_BLEND_FACTOR_MAX_ENUM_FB = 0x7FFFFFFF
    };

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct XrCompositionLayerAlphaBlendFB
    {
        public XrStructureType type;
        public void* next;
        public XrBlendFactorFB srcFactorColor;
        public XrBlendFactorFB dstFactorColor;
        public XrBlendFactorFB srcFactorAlpha;
        public XrBlendFactorFB dstFactorAlpha;

        public XrCompositionLayerAlphaBlendFB(XrBlendFactorFB srcFactorColor, XrBlendFactorFB dstFactorColor, XrBlendFactorFB srcFactorAlpha, XrBlendFactorFB dstFactorAlpha)
        {
            type = XrStructureTypeExt.XR_TYPE_COMPOSITION_LAYER_ALPHA_BLEND_FB;
            next = null;
            this.srcFactorColor = srcFactorColor;
            this.dstFactorColor = dstFactorColor;
            this.srcFactorAlpha = srcFactorAlpha;
            this.dstFactorAlpha = dstFactorAlpha;
        }

        public XrCompositionLayerAlphaBlendFB(XrBlendFactorFB srcFactorColor, XrBlendFactorFB dstFactorColor)
        {
            type = XrStructureTypeExt.XR_TYPE_COMPOSITION_LAYER_ALPHA_BLEND_FB;
            next = null;
            this.srcFactorColor = srcFactorColor;
            this.dstFactorColor = dstFactorColor;
            srcFactorAlpha = XrBlendFactorFB.XR_BLEND_FACTOR_ONE_FB;
            dstFactorAlpha = XrBlendFactorFB.XR_BLEND_FACTOR_ZERO_FB;
        }
    };
}
