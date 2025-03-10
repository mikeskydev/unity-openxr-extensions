// SPDX-FileCopyrightText: 2024 Michael Nisbet <me@mikesky.dev>
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.InteropServices;

namespace UnityEngine.XR.OpenXR.NativeTypes
{
    // Placeholders where not defined in main OpenXR plugin
    [StructLayout(LayoutKind.Sequential)] public struct XrBool32
    {
        public UInt32 value;
        public XrBool32(bool value) => this.value = value ? 1u : 0u;
    }

    [StructLayout(LayoutKind.Sequential)] public struct XrTime { public Int64 value; }
    [StructLayout(LayoutKind.Sequential)] public struct XrDuration { public Int64 value; }

    [StructLayout(LayoutKind.Sequential)] public struct XrInstance { public UInt64 handle; }
    [StructLayout(LayoutKind.Sequential)] public struct XrSession { public UInt64 handle; }
    [StructLayout(LayoutKind.Sequential)] public struct XrSpace { public UInt64 handle; }
    [StructLayout(LayoutKind.Sequential)] public struct XrAction { public UInt64 handle; }
    [StructLayout(LayoutKind.Sequential)] public struct XrSwapchain { public UInt64 handle; }
    [StructLayout(LayoutKind.Sequential)] public struct XrActionSet { public UInt64 handle; }


    public class Constants
    {
        public const uint XR_TRUE = 1;
        public const uint XR_FALSE = 0;
        public const int XR_MAX_EXTENSION_NAME_SIZE = 128;
        public const int XR_MAX_API_LAYER_NAME_SIZE = 256;
        public const int XR_MAX_API_LAYER_DESCRIPTION_SIZE = 256;
        public const int XR_MAX_SYSTEM_NAME_SIZE = 256;
        public const int XR_MAX_APPLICATION_NAME_SIZE = 128;
        public const int XR_MAX_ENGINE_NAME_SIZE = 128;
        public const int XR_MAX_RUNTIME_NAME_SIZE = 128;
        public const int XR_MAX_PATH_LENGTH = 256;
        public const int XR_MAX_STRUCTURE_NAME_SIZE = 64;
        public const int XR_MAX_RESULT_STRING_SIZE = 64;
        public const int XR_MAX_ACTION_SET_NAME_SIZE = 64;
        public const int XR_MAX_LOCALIZED_ACTION_SET_NAME_SIZE = 128;
        public const int XR_MAX_ACTION_NAME_SIZE = 4;
        public const int XR_MAX_LOCALIZED_ACTION_NAME_SIZE = 128;
        public const int XR_PASSTHROUGH_COLOR_MAP_MONO_SIZE_FB = 256;
    }

    public enum XrObjectType
    {

    }

    public enum XrFormFactor : UInt64
    {
        XR_FORM_FACTOR_HEAD_MOUNTED_DISPLAY = 1,
        XR_FORM_FACTOR_HANDHELD_DISPLAY = 2,
        XR_FORM_FACTOR_MAX_ENUM = 0x7FFFFFFF
    }

    // Extras

    // This is NOT specified as XrFlags64 (64 bit) in the main OpenXR plugin, causes alingment issues
    [Flags]
    public enum XrSpaceLocationFlagsExt : UInt64
    {
        XR_SPACE_LOCATION_ORIENTATION_VALID_BIT = 1,
        XR_SPACE_LOCATION_POSITION_VALID_BIT = 2,
        XR_SPACE_LOCATION_ORIENTATION_TRACKED_BIT = 4,
        XR_SPACE_LOCATION_POSITION_TRACKED_BIT = 8
    }

    // Specified as [Flags] in the main OpenXR plugin, wrong
    public enum XrReferenceSpaceTypeExt
    {
        XR_REFERENCE_SPACE_TYPE_VIEW = 1,
        XR_REFERENCE_SPACE_TYPE_LOCAL = 2,
        XR_REFERENCE_SPACE_TYPE_STAGE = 3,
        XR_REFERENCE_SPACE_TYPE_UNBOUNDED_MSFT = 1000038000,
        XR_REFERENCE_SPACE_TYPE_COMBINED_EYE_VARJO = 1000121000,

        XR_REFERENCE_SPACE_TYPE_LOCALIZATION_MAP_ML = 1000139000,
        XR_REFERENCE_SPACE_TYPE_LOCAL_FLOOR_EXT = 1000426000,
        XR_REFERENCE_SPACE_TYPE_MAX_ENUM = 0x7FFFFFFF
    }


    public class XrResultExt
    {
        public const XrResult XR_ERROR_DISPLAY_REFRESH_RATE_UNSUPPORTED_FB = (XrResult)(-1000101000);
        public const XrResult XR_ERROR_UNEXPECTED_STATE_PASSTHROUGH_FB = (XrResult)(-1000118000);
        public const XrResult XR_ERROR_FEATURE_ALREADY_CREATED_PASSTHROUGH_FB = (XrResult)(-1000118001);
        public const XrResult XR_ERROR_FEATURE_REQUIRED_PASSTHROUGH_FB = (XrResult)(-1000118002);
        public const XrResult XR_ERROR_NOT_PERMITTED_PASSTHROUGH_FB = (XrResult)(-1000118003);
        public const XrResult XR_ERROR_INSUFFICIENT_RESOURCES_PASSTHROUGH_FB = (XrResult)(-1000118004);
        public const XrResult XR_ERROR_UNKNOWN_PASSTHROUGH_FB = (XrResult)(-1000118050);
        public const XrResult XR_BOUNDARY_VISIBILITY_SUPPRESSION_NOT_ALLOWED_META  = (XrResult)1000528000;
    }

    public class XrStructureTypeExt
    {
        public const XrStructureType XR_TYPE_SYSTEM_GET_INFO = (XrStructureType)4;
        public const XrStructureType XR_TYPE_SYSTEM_PROPERTIES = (XrStructureType)5;
        public const XrStructureType XR_TYPE_REFERENCE_SPACE_CREATE_INFO = (XrStructureType)37;
        public const XrStructureType XR_TYPE_COMPOSITION_LAYER_ALPHA_BLEND_FB = (XrStructureType)1000041001;
        public const XrStructureType XR_TYPE_BODY_TRACKER_CREATE_INFO_FB = (XrStructureType)1000076001;
        public const XrStructureType XR_TYPE_BODY_JOINTS_LOCATE_INFO_FB = (XrStructureType)1000076002;
        public const XrStructureType XR_TYPE_SYSTEM_BODY_TRACKING_PROPERTIES_FB = (XrStructureType)1000076004;
        public const XrStructureType XR_TYPE_BODY_JOINT_LOCATIONS_FB = (XrStructureType)1000076005;
        public const XrStructureType XR_TYPE_BODY_SKELETON_FB = (XrStructureType)1000076006;
        public const XrStructureType XR_TYPE_EVENT_DATA_DISPLAY_REFRESH_RATE_CHANGED_FB = (XrStructureType)1000101000;
        public const XrStructureType XR_TYPE_SYSTEM_PASSTHROUGH_PROPERTIES_FB = (XrStructureType)1000118000;
        public const XrStructureType XR_TYPE_PASSTHROUGH_CREATE_INFO_FB = (XrStructureType)1000118001;
        public const XrStructureType XR_TYPE_PASSTHROUGH_LAYER_CREATE_INFO_FB = (XrStructureType)1000118002;
        public const XrStructureType XR_TYPE_COMPOSITION_LAYER_PASSTHROUGH_FB = (XrStructureType)1000118003;
        public const XrStructureType XR_TYPE_GEOMETRY_INSTANCE_CREATE_INFO_FB = (XrStructureType)1000118004;
        public const XrStructureType XR_TYPE_GEOMETRY_INSTANCE_TRANSFORM_FB = (XrStructureType)1000118005;
        public const XrStructureType XR_TYPE_SYSTEM_PASSTHROUGH_PROPERTIES2_FB = (XrStructureType)1000118006;
        public const XrStructureType XR_TYPE_PASSTHROUGH_STYLE_FB = (XrStructureType)1000118020;
        public const XrStructureType XR_TYPE_PASSTHROUGH_COLOR_MAP_MONO_TO_RGBA_FB = (XrStructureType)1000118021;
        public const XrStructureType XR_TYPE_PASSTHROUGH_COLOR_MAP_MONO_TO_MONO_FB = (XrStructureType)1000118022;
        public const XrStructureType XR_TYPE_PASSTHROUGH_BRIGHTNESS_CONTRAST_SATURATION_FB = (XrStructureType)1000118023;
        public const XrStructureType XR_TYPE_EVENT_DATA_PASSTHROUGH_STATE_CHANGED_FB = (XrStructureType)1000118030;
        public const XrStructureType XR_TYPE_COMPOSITION_LAYER_DEPTH_TEST_FB = (XrStructureType)1000212000;
        public const XrStructureType XR_TYPE_SYSTEM_PROPERTIES_BODY_TRACKING_FULL_BODY_META = (XrStructureType) 1000274000;
        public const XrStructureType XR_TYPE_SYSTEM_BOUNDARY_VISIBILITY_PROPERTIES_META = (XrStructureType)1000528000;
        public const XrStructureType XR_TYPE_EVENT_DATA_BOUNDARY_VISIBILITY_CHANGED_META = (XrStructureType)1000528001;
        public const XrStructureType XR_STRUCTURE_TYPE_MAX_ENUM = (XrStructureType)0x7FFFFFFF;
    }

    public class XrObjectTypeExt
    {
        public const XrObjectType XR_OBJECT_TYPE_BODY_TRACKER_FB = (XrObjectType)1000076000;
    }

    public class XrCompositionLayerFlagsExt
    {
        public const XrCompositionLayerFlags None = (XrCompositionLayerFlags)0;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XrColor4f
    {
        public float r;
        public float g;
        public float b;
        public float a;

        public XrColor4f(Color color)
        {
            r = color.r;
            g = color.g;
            b = color.b;
            a = color.a;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct XrFrameWaitInfo
    {
        public XrStructureType type;
        public void* next;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct XrFrameState
    {
        public XrStructureType type;
        public void* next;
        public XrTime predictedDisplayTime;
        public Int64 predictedDisplayPeriod;
        public XrBool32 shouldRender;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct XrFrameEndInfo
    {
        public XrStructureType type;
        public void* next;
        public XrTime displayTime;
        public XrEnvironmentBlendMode environmentBlendMode;
        public UInt32 layerCount;
        public XrCompositionLayerBaseHeader** layers;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct XrEventDataBuffer
    {
        public XrStructureType type;
        public void* next;
        public fixed byte varying[4000];
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct XrSystemGetInfo
    {
        public XrStructureType type;
        public void* next;
        public XrFormFactor formFactor;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct XrSystemProperties
    {
        public XrStructureType type;
        public void* next;
        public UInt64 systemId;
        public UInt32 vendorId;
        public fixed char systemName[Constants.XR_MAX_SYSTEM_NAME_SIZE];
        public XrSystemGraphicsProperties graphicsProperties;
        public XrSystemTrackingProperties trackingProperties;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XrSystemGraphicsProperties
    {
        public UInt32 maxSwapchainImageHeight;
        public UInt32 maxSwapchainImageWidth;
        public UInt32 maxLayerCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XrSystemTrackingProperties
    {
        XrBool32 orientationTracking;
        XrBool32 positionTracking;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct XrReferenceSpaceCreateInfo
    {
        public XrStructureType type;
        public void* next;
        public XrReferenceSpaceTypeExt referenceSpaceType;
        public XrPosef poseInReferenceSpace;
    }

    public delegate XrResult del_xrGetInstanceProcAddr(UInt64 instance, string name, ref IntPtr function);
    unsafe delegate XrResult del_xrWaitFrame(UInt64 xrSession, XrFrameWaitInfo* frameWaitInfo, XrFrameState* frameState);
    unsafe delegate XrResult del_xrEndFrame(UInt64 xrSession, XrFrameEndInfo* frameEndInfo);
    unsafe delegate XrResult del_xrGetSystem(UInt64 instance, XrSystemGetInfo* getInfo, UInt64* systemId);
    unsafe delegate XrResult del_xrGetSystemProperties(UInt64 instance, UInt64 systemId, XrSystemProperties* properties);
    unsafe delegate XrResult del_xrPollEvent(UInt64 instance, XrEventDataBuffer* eventData);
    unsafe delegate XrResult del_xrCreateReferenceSpace(UInt64 session, XrReferenceSpaceCreateInfo* createInfo, XrSpace* space);
}