// SPDX-FileCopyrightText: 2024 Michael Nisbet <me@mikesky.dev>
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.CompositionLayers;
using UnityEngine.XR.OpenXR.NativeTypes;
using AOT;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
#endif

namespace OpenXR.Extensions
{
#if UNITY_EDITOR
    [OpenXRFeature(UiName = "FB Passthrough",
        BuildTargetGroups = new [] { BuildTargetGroup.Standalone, BuildTargetGroup.WSA, BuildTargetGroup.Android },
        Company = "Mikesky",
        Desc = "Implement XR_FB_passthrough",
        OpenxrExtensionStrings = XR_FB_PASSTHROUGH,
        Version = "0.1.0",
        FeatureId = FeatureId)]
#endif
    public class FBPassthrough : FeatureBase<FBPassthrough>
    {
        public const string XR_FB_PASSTHROUGH = "XR_FB_passthrough";
        public const string FeatureId = "dev.mikesky.openxr.extensions.fbpassthrough";

        static NativeArray<XrCompositionLayerPassthroughFB> layersNA;
        static NativeArray<int> layersSortOrderNA;

        public bool StartEnabled;
        private static bool _implAvailable;
        private static bool _implEnabled;
        private static XrPassthroughFB activePassthrough;// = new XrPassthroughFB();
        private static XrPassthroughLayerFB activeLayer;// = new XrPassthroughLayerFB();

        public static bool Available => _implAvailable;

        // Public toggle
        public static bool ExtEnabled
        {
            get => _implEnabled;

            set
            {
                if (_implAvailable == false || _implEnabled == value)
                {
                    return;
                }

                if (value)
                {
                    _implEnabled = StartPassthrough();
                }
                else
                {
                    PausePassthrough();
                    _implEnabled = false;
                }
            }
        }

        bool InitPassthrough()
        {
            // If creation bit is not supplied, the state will be implied as paused
            XrPassthroughFlagsFB flags = StartEnabled
                ? XrPassthroughFlagsFB.XR_PASSTHROUGH_IS_RUNNING_AT_CREATION_BIT_FB
                : XrPassthroughFlagsFB.None;

            XrResult result = xrCreatePassthroughFB(
                XrSession,
                new XrPassthroughCreateInfoFB(flags),
                out activePassthrough);
            if (result != XrResult.Success)
            {
                Debug.LogError($"xrCreatePassthroughFB failed: {result}");
                return false;
            }

            result = xrCreatePassthroughLayerFB(
                XrSession,
                new XrPassthroughLayerCreateInfoFB(activePassthrough, flags, XrPassthroughLayerPurposeFB.XR_PASSTHROUGH_LAYER_PURPOSE_RECONSTRUCTION_FB),
                out activeLayer);
            if (result != XrResult.Success)
            {
                Debug.LogError($"xrCreatePassthroughLayerFB failed: {result}");
                return false;
            }

            _implEnabled = StartEnabled;
            return true;
        }

        static bool StartPassthrough()
        {
            XrResult result = xrPassthroughStartFB(activePassthrough);
            if (result != XrResult.Success)
            {
                Debug.LogError($"xrPassthroughStartFB failed: {result}");
                return false;
            }

            result = xrPassthroughLayerResumeFB(activeLayer);
            if (result != XrResult.Success)
            {
                Debug.LogError($"xrPassthroughLayerResumeFB failed: {result}");
                return false;
            }

            return true;
        }

        static void PausePassthrough()
        {
            XrResult result = xrPassthroughLayerPauseFB(activeLayer);
            if (result != XrResult.Success)
            {
                Debug.LogError($"xrPassthroughLayerPauseFB failed: {result}");
                return;
            }

            result = xrPassthroughPauseFB(activePassthrough);
            if (result != XrResult.Success)
            {
                Debug.LogError($"xrPassthroughPauseFB failed: {result}");
                return;
            }
        }

        public static void OnUpdate()
        {
            if (ExtEnabled == false) return;

            XrCompositionLayerPassthroughFB layer = new XrCompositionLayerPassthroughFB(XrCompositionLayerFlags.SourceAlpha, activeLayer);

            // This is the sort layer
            int sortLayerTarget = -1;

            layersNA[0] = layer;
            layersSortOrderNA[0] = sortLayerTarget;
            unsafe
            {
                OpenXRLayerUtility.AddActiveLayersToEndFrame(layersNA.GetUnsafePtr(), layersSortOrderNA.GetUnsafePtr(), layersNA.Length, UnsafeUtility.SizeOf<XrCompositionLayerPassthroughFB>());
            }
        }


        #region Unity OpenXR Impl
        /// <inheritdoc />

        /// <inheritdoc />
        protected override void OnSessionCreate(ulong xrSession)
        {
            base.OnSessionCreate(xrSession);

            _implAvailable = OpenXRRuntime.IsExtensionEnabled(XR_FB_PASSTHROUGH) && InitPassthrough();

            layersNA = new NativeArray<XrCompositionLayerPassthroughFB>(1, Allocator.Persistent);
            layersSortOrderNA = new NativeArray<int>(1, Allocator.Persistent);
        }

        protected override void OnSessionDestroy(ulong xrSession)
        {
            base.OnSessionDestroy(xrSession);

            if (layersNA.IsCreated)
                layersNA.Dispose();
            if (layersSortOrderNA.IsCreated)
                layersSortOrderNA.Dispose();

            xrDestroyPassthroughLayerFB(activeLayer);
            xrDestroyPassthroughFB(activePassthrough);
        }

        protected override void OnSessionEnd(ulong xrSession)
        {
            base.OnSessionEnd(xrSession);
            if (!ExtEnabled) return;
            ExtEnabled = false;
        }

        #endregion

        #region OpenXR native bindings
        private static del_xrCreatePassthroughFB xrCreatePassthroughFB;
        private static del_xrDestroyPassthroughFB xrDestroyPassthroughFB;
        private static del_xrPassthroughStartFB xrPassthroughStartFB;
        private static del_xrPassthroughPauseFB xrPassthroughPauseFB;
        private static del_xrCreatePassthroughLayerFB xrCreatePassthroughLayerFB;
        private static del_xrDestroyPassthroughLayerFB xrDestroyPassthroughLayerFB;
        private static del_xrPassthroughLayerPauseFB xrPassthroughLayerPauseFB;
        private static del_xrPassthroughLayerResumeFB xrPassthroughLayerResumeFB;
        private static del_xrPassthroughLayerSetStyleFB xrPassthroughLayerSetStyleFB;

        protected override bool CheckRequiredExtensions()
        {
            return OpenXRRuntime.IsExtensionEnabled(XR_FB_PASSTHROUGH);
        }

        protected unsafe override bool LoadBindings()
        {
            try
            {
                HookFunction("xrCreatePassthroughFB", ref xrCreatePassthroughFB);
                HookFunction("xrDestroyPassthroughFB", ref xrDestroyPassthroughFB);
                HookFunction("xrPassthroughStartFB", ref xrPassthroughStartFB);
                HookFunction("xrPassthroughPauseFB", ref xrPassthroughPauseFB);
                HookFunction("xrCreatePassthroughLayerFB", ref xrCreatePassthroughLayerFB);
                HookFunction("xrDestroyPassthroughLayerFB", ref xrDestroyPassthroughLayerFB);
                HookFunction("xrPassthroughLayerPauseFB", ref xrPassthroughLayerPauseFB);
                HookFunction("xrPassthroughLayerResumeFB", ref xrPassthroughLayerResumeFB);
                HookFunction("xrPassthroughLayerSetStyleFB", ref xrPassthroughLayerSetStyleFB);
            }
            catch (GetInstanceProcAddrException e)
            {
                Debug.LogWarning(e.Message);
                return false;
            }
            return
                xrCreatePassthroughFB != null &&
                xrDestroyPassthroughFB != null &&
                xrPassthroughStartFB != null &&
                xrPassthroughPauseFB != null &&
                xrCreatePassthroughLayerFB != null &&
                xrDestroyPassthroughLayerFB != null &&
                xrPassthroughLayerPauseFB != null &&
                xrPassthroughLayerResumeFB != null &&
                xrPassthroughLayerSetStyleFB != null &&
                true;
        }
        #endregion
    }
}

namespace UnityEngine.XR.OpenXR.NativeTypes
{
#pragma warning disable 0169 // handle is not "used", but required for interop
    [StructLayout(LayoutKind.Sequential)] public struct XrPassthroughFB { public UInt64 handle; }
    [StructLayout(LayoutKind.Sequential)] public struct XrPassthroughLayerFB { public UInt64 handle; }
    [StructLayout(LayoutKind.Sequential)] public struct XrGeometryInstanceFB { public UInt64 handle; }
#pragma warning restore 0169

    [Flags]
    public enum XrPassthroughFlagsFB : ulong
    {
        None = 0,
        XR_PASSTHROUGH_IS_RUNNING_AT_CREATION_BIT_FB = 0x00000001,
        XR_PASSTHROUGH_LAYER_DEPTH_BIT_FB = 0x00000002,
    }

    [Flags]
    public enum XrPassthroughStateChangedFlagsFB : ulong
    {
        XR_PASSTHROUGH_STATE_CHANGED_REINIT_REQUIRED_BIT_FB = 0x00000001,
        XR_PASSTHROUGH_STATE_CHANGED_NON_RECOVERABLE_ERROR_BIT_FB = 0x00000002,
        XR_PASSTHROUGH_STATE_CHANGED_RECOVERABLE_ERROR_BIT_FB = 0x00000004,
        XR_PASSTHROUGH_STATE_CHANGED_RESTORED_ERROR_BIT_FB = 0x00000008,
    }

    [Flags]
    public enum XrPassthroughCapabilityFlagsFB : ulong
    {
        XR_PASSTHROUGH_CAPABILITY_BIT_FB = 0x00000001,
        XR_PASSTHROUGH_CAPABILITY_COLOR_BIT_FB = 0x00000002,
        XR_PASSTHROUGH_CAPABILITY_LAYER_DEPTH_BIT_FB = 0x00000004,
    }

    public enum XrPassthroughLayerPurposeFB : uint
    {
        XR_PASSTHROUGH_LAYER_PURPOSE_RECONSTRUCTION_FB = 0,
        XR_PASSTHROUGH_LAYER_PURPOSE_PROJECTED_FB = 1,
        XR_PASSTHROUGH_LAYER_PURPOSE_TRACKED_KEYBOARD_HANDS_FB = 1000203001,
        XR_PASSTHROUGH_LAYER_PURPOSE_TRACKED_KEYBOARD_MASKED_HANDS_FB = 1000203002,
        XR_PASSTHROUGH_LAYER_PURPOSE_MAX_ENUM_FB = 0x7FFFFFFF,
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct XrSystemPassthroughPropertiesFB
    {
        public XrStructureType type;
        public void* next;
        public XrBool32 supportsPassthrough;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe  struct XrSystemPassthroughProperties2FB
    {
        public XrStructureType type;
        public void* next;
        public XrPassthroughCapabilityFlagsFB capabilities;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct XrPassthroughCreateInfoFB
    {
        public XrStructureType type;
        public void* next;
        public XrPassthroughFlagsFB flags;

        public XrPassthroughCreateInfoFB(XrPassthroughFlagsFB passthroughFlags)
        {
            type = XrStructureTypeExt.XR_TYPE_PASSTHROUGH_CREATE_INFO_FB;
            next = null;
            flags = passthroughFlags;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct XrPassthroughLayerCreateInfoFB
    {
        public XrStructureType type;
        public void* next;
        public XrPassthroughFB passthrough;
        public XrPassthroughFlagsFB flags;
        public XrPassthroughLayerPurposeFB purpose;

        public XrPassthroughLayerCreateInfoFB(XrPassthroughFB passthrough, XrPassthroughFlagsFB flags, XrPassthroughLayerPurposeFB purpose)
        {
            type = XrStructureTypeExt.XR_TYPE_PASSTHROUGH_LAYER_CREATE_INFO_FB;
            next = null;
            this.passthrough = passthrough;
            this.flags = flags;
            this.purpose = purpose;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct XrCompositionLayerPassthroughFB
    {
        public XrStructureType type;
        public void* next;
        public XrCompositionLayerFlags flags;
        public XrSpace space;
        public XrPassthroughLayerFB layerHandle;
        public XrCompositionLayerPassthroughFB(XrCompositionLayerFlags flags, XrPassthroughLayerFB layerHandle)
        {
            type = XrStructureTypeExt.XR_TYPE_COMPOSITION_LAYER_PASSTHROUGH_FB;
            next = null;
            space = new XrSpace { handle = 0 };
            this.flags = flags;
            this.layerHandle = layerHandle;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct XrPassthroughStyleFB
    {
        public XrStructureType type;
        public void* next;
        public float textureOpacityFactor;
        public XrColor4f edgeColor;
        public XrPassthroughStyleFB(float textureOpacityFactor, Color edgeColor)
        {
            type = XrStructureTypeExt.XR_TYPE_PASSTHROUGH_STYLE_FB;
            next = null;
            this.textureOpacityFactor = textureOpacityFactor;
            this.edgeColor = new XrColor4f(edgeColor);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct XrPassthroughColorMapMonoToRgbaFB
    {
        public XrStructureType type;
        public void* next;
        public XrColor4f[] textureColorMap;

        public XrPassthroughColorMapMonoToRgbaFB(Color[] textureColorMap)
        {
            type = XrStructureTypeExt.XR_TYPE_PASSTHROUGH_COLOR_MAP_MONO_TO_RGBA_FB;
            next = null;
            this.textureColorMap = new XrColor4f[Constants.XR_PASSTHROUGH_COLOR_MAP_MONO_SIZE_FB];
            for (int i = 0; i < textureColorMap.Length; i++)
            {
                this.textureColorMap[i] = new XrColor4f(textureColorMap[i]);
            }
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct XrPassthroughColorMapMonoToMonoFB
    {
        public XrStructureType type;
        public void* next;
        public byte[] textureColorMap;

        public XrPassthroughColorMapMonoToMonoFB(byte[] textureColorMap)
        {
            type = XrStructureTypeExt.XR_TYPE_PASSTHROUGH_COLOR_MAP_MONO_TO_MONO_FB;
            next = null;
            this.textureColorMap = new byte[Constants.XR_PASSTHROUGH_COLOR_MAP_MONO_SIZE_FB];
            for (int i = 0; i < textureColorMap.Length; i++)
            {
                this.textureColorMap[i] = textureColorMap[i];
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct XrPassthroughBrightnessContrastSaturationFB
    {
        public XrStructureType type;
        public void* next;
        public float brightness;
        public float contrast;
        public float saturation;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct XrEventDataPassthroughStateChangedFB
    {
        public XrStructureType type;
        public void* next;
        XrPassthroughStateChangedFlagsFB flags;
    }

    delegate XrResult del_xrCreatePassthroughFB(UInt64 session, [In] XrPassthroughCreateInfoFB createInfo, out XrPassthroughFB outPassthrough);
    delegate XrResult del_xrDestroyPassthroughFB(XrPassthroughFB passthrough);
    delegate XrResult del_xrPassthroughStartFB(XrPassthroughFB passthrough);
    delegate XrResult del_xrPassthroughPauseFB(XrPassthroughFB passthrough);
    delegate XrResult del_xrCreatePassthroughLayerFB(UInt64 session, [In] XrPassthroughLayerCreateInfoFB createInfo, out XrPassthroughLayerFB outLayer);
    delegate XrResult del_xrDestroyPassthroughLayerFB(XrPassthroughLayerFB layer);
    delegate XrResult del_xrPassthroughLayerPauseFB(XrPassthroughLayerFB layer);
    delegate XrResult del_xrPassthroughLayerResumeFB(XrPassthroughLayerFB layer);
    delegate XrResult del_xrPassthroughLayerSetStyleFB(XrPassthroughLayerFB layer, [In] XrPassthroughStyleFB style);
}
