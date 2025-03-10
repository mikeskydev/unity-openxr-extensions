// SPDX-FileCopyrightText: 2024 Michael Nisbet <me@mikesky.dev>
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.NativeTypes;
using AOT;
using System.IO;
using System.Xml;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
#endif

namespace OpenXR.Extensions
{
#if UNITY_EDITOR
    [OpenXRFeature(UiName = "FB Body Tracking + META Body Tracking Full Body",
        BuildTargetGroups = new [] {BuildTargetGroup.Standalone, BuildTargetGroup.WSA, BuildTargetGroup.Android},
        Company = "Mikesky",
        Desc = "Implement XR_FB_body_tracking and XR_META_body_tracking_full_body",
        OpenxrExtensionStrings = XR_FB_BODY_TRACKING + " " + XR_META_BODY_TRACKING_FULL,
        Version = "0.1.0",
        FeatureId = FeatureId,
        Priority = 1)]
#endif
    public class FBBodyTracking : FeatureBase<FBBodyTracking>
    {
        public const string XR_FB_BODY_TRACKING = "XR_FB_body_tracking";
        public const string XR_META_BODY_TRACKING_FULL = "XR_META_body_tracking_full_body";
        public const string FeatureId = "dev.mikesky.openxr.extensions.fbbodytracking";

        public bool RequiredFeature;

        [Flags]
        public enum TrackingType
        {
            Torso,
            Full
        }

        public TrackingType _TrackingType;

        public static bool BodyTrackingSupported { get; private set; }
        public static bool FullBodyTrackingSupported { get; private set; }
        public static bool TrackingActive { get; private set; }
        public static int AvailableJointCount { get; private set; }

        public static XrBodyJointLocationFB[] JointLocations { get; private set; }
        public static XrBodySkeletonJointFB[] SkeletonJoints { get; private set; }

        private static UInt64 SystemId;
        private static XrSpace SpaceId;
        private static XrFrameState FrameState;
        private static UInt64 bodyTrackerHandle;


        private static XrBodySkeletonFB skeleton;
        private static uint skeletonChangedCount;

        protected override void OnSessionCreate(ulong xrSession)
        {
            base.OnSessionCreate(xrSession);

            bool MetaBodyTrackingFullEnabled = OpenXRRuntime.IsExtensionEnabled(XR_META_BODY_TRACKING_FULL) && _TrackingType == TrackingType.Full;

            unsafe
            {
                XrSystemProperties systemProperties = new XrSystemProperties { type = XrStructureTypeExt.XR_TYPE_SYSTEM_PROPERTIES };
                XrSystemBodyTrackingPropertiesFB bodyTrackingSystemProperties = new XrSystemBodyTrackingPropertiesFB { type = XrStructureTypeExt.XR_TYPE_SYSTEM_BODY_TRACKING_PROPERTIES_FB };
                XrSystemPropertiesBodyTrackingFullBodyMETA fullBodyTrackingSystemProperties = new XrSystemPropertiesBodyTrackingFullBodyMETA { type = XrStructureTypeExt.XR_TYPE_SYSTEM_PROPERTIES_BODY_TRACKING_FULL_BODY_META };

                systemProperties.next = &bodyTrackingSystemProperties;

                if (MetaBodyTrackingFullEnabled)
                {
                    bodyTrackingSystemProperties.next = &fullBodyTrackingSystemProperties;
                }

                var xrResult = xrGetSystemProperties(XrInstance, SystemId, &systemProperties);
                
                BodyTrackingSupported = bodyTrackingSystemProperties.supportsBodyTracking.value == 1;
                FullBodyTrackingSupported = fullBodyTrackingSystemProperties.supportsFullBodyTracking.value == 1;

                Debug.Log("Body tracking supported: " + BodyTrackingSupported);
                Debug.Log("Full body tracking supported: " + FullBodyTrackingSupported);

                AvailableJointCount = FullBodyTrackingSupported ? (int)XrFullBodyJointMETA.XR_FULL_BODY_JOINT_COUNT_META : BodyTrackingSupported ? (int)XrBodyJointFB.XR_BODY_JOINT_COUNT_FB : 0;

                if (BodyTrackingSupported)
                {
                    JointLocations = new XrBodyJointLocationFB[AvailableJointCount];
                    SkeletonJoints = new XrBodySkeletonJointFB[AvailableJointCount];
                }
            }
        }

        protected override void OnSessionDestroy(ulong xrSession)
        {
            if (bodyTrackerHandle != 0)
                xrDestroyBodyTrackerFB(bodyTrackerHandle);
            BodyTrackingSupported = false;
            FullBodyTrackingSupported = false;
        }

        protected override void OnInstanceDestroy(ulong xrInstance)
        {
            xrCreateBodyTrackerFB = null;
            xrDestroyBodyTrackerFB = null;
            xrLocateBodyJointsFB = null;
            xrGetBodySkeletonFB = null;
        }

        public static bool Init()
        {
            if (!BodyTrackingSupported) return false;

            unsafe
            {
                XrBodyTrackerCreateInfoFB bodyTrackerCreateInfo = new XrBodyTrackerCreateInfoFB
                {
                    type = XrStructureTypeExt.XR_TYPE_BODY_TRACKER_CREATE_INFO_FB,
                    bodyJointSet = FullBodyTrackingSupported ? XrBodyJointSetFB.XR_BODY_JOINT_SET_FULL_BODY_META :  XrBodyJointSetFB.XR_BODY_JOINT_SET_DEFAULT_FB
                };

                XrResult result;
                result = xrCreateBodyTrackerFB(XrSession, &bodyTrackerCreateInfo, out bodyTrackerHandle);
            }

            return true;
        }

        public static void Update()
        {
            if (!BodyTrackingSupported) return;
            unsafe
            {
                if (bodyTrackerHandle != 0)
                {
                    var locateInfo = new XrBodyJointsLocateInfoFB
                    {
                        type = XrStructureTypeExt.XR_TYPE_BODY_JOINTS_LOCATE_INFO_FB,
                        baseSpace = SpaceId,
                        time = FrameState.predictedDisplayTime
                    };

                    XrBodyJointLocationsFB locations = new XrBodyJointLocationsFB { type = XrStructureTypeExt.XR_TYPE_BODY_JOINT_LOCATIONS_FB };
                    locations.jointCount = (uint)AvailableJointCount;

                    GCHandle jointsHandle = GCHandle.Alloc(JointLocations, GCHandleType.Pinned);
                    locations.jointLocations = (XrBodyJointLocationFB*)jointsHandle.AddrOfPinnedObject();

                    var result = xrLocateBodyJointsFB(bodyTrackerHandle, &locateInfo, &locations);
                    TrackingActive = locations.isActive.value == 1;

                    jointsHandle.Free();

                    if (locations.skeletonChangedCount != skeletonChangedCount)
                    {
                        skeletonChangedCount = locations.skeletonChangedCount;

                        var skeleton = new XrBodySkeletonFB
                        {
                            type = XrStructureTypeExt.XR_TYPE_BODY_SKELETON_FB,
                            jointCount = (uint)AvailableJointCount
                        };

                        GCHandle skelHandle = GCHandle.Alloc(SkeletonJoints, GCHandleType.Pinned);
                        skeleton.joints = (XrBodySkeletonJointFB*)skelHandle.AddrOfPinnedObject();
                        result = xrGetBodySkeletonFB(bodyTrackerHandle, &skeleton);
                        skelHandle.Free();
                    }

                }
            }
        }

#region OpenXR native bindings

        static del_xrCreateBodyTrackerFB xrCreateBodyTrackerFB;
        static del_xrDestroyBodyTrackerFB xrDestroyBodyTrackerFB;
        static del_xrLocateBodyJointsFB xrLocateBodyJointsFB;
        static del_xrGetBodySkeletonFB xrGetBodySkeletonFB;

        static del_xrGetSystemProperties xrGetSystemProperties;

        static del_xrGetSystem intercept_xrGetSystem;
        static del_xrWaitFrame intercept_xrWaitFrame;

        [MonoPInvokeCallback(typeof(del_xrGetSystem))]
        private unsafe static XrResult Intercepted_xrGetSystem(UInt64 instance, XrSystemGetInfo* getInfo, UInt64* systemId)
        {
            var result = intercept_xrGetSystem(instance, getInfo, systemId);
            SystemId = *systemId;

            return result;
        }

        protected override void OnAppSpaceChange(ulong xrSpace)
        {
            SpaceId = new XrSpace { handle = xrSpace };
        }

        [MonoPInvokeCallback(typeof(del_xrWaitFrame))]
        private unsafe static XrResult Intercepted_xrWaitFrame(UInt64 xrSession, XrFrameWaitInfo* frameWaitInfo, XrFrameState* frameState)
        {
            var result = intercept_xrWaitFrame(xrSession, frameWaitInfo, frameState);
            FrameState = *frameState;
            return result;
        }

        protected unsafe override IntPtr HookGetInstanceProcAddr(IntPtr xrGetInstanceProcAddr)
        {
            OnGetInstanceProcAddr += Intercept;
            return base.HookGetInstanceProcAddr(xrGetInstanceProcAddr);
        }

        protected unsafe static XrResult Intercept(ulong instance, string requestedFunctionName, ref IntPtr outgoingFunctionPointer)
        {
            InterceptFunction("xrGetSystem", Intercepted_xrGetSystem, ref intercept_xrGetSystem, requestedFunctionName, ref outgoingFunctionPointer);
            InterceptFunction("xrWaitFrame", Intercepted_xrWaitFrame, ref intercept_xrWaitFrame, requestedFunctionName, ref outgoingFunctionPointer);
            return XrResult.Success;
        }

        protected override bool CheckRequiredExtensions()
        {
            // Note: does not need META_body_tracking_full_body.
            return OpenXRRuntime.IsExtensionEnabled(XR_FB_BODY_TRACKING);
        }

        protected unsafe override bool HookFunctions()
        {
            try
            {
                HookFunction("xrGetSystemProperties", ref xrGetSystemProperties);
                HookFunction("xrCreateBodyTrackerFB", ref xrCreateBodyTrackerFB);
                HookFunction("xrDestroyBodyTrackerFB", ref xrDestroyBodyTrackerFB);
                HookFunction("xrLocateBodyJointsFB", ref xrLocateBodyJointsFB);
                HookFunction("xrGetBodySkeletonFB", ref xrGetBodySkeletonFB);
            }
            catch (GetInstanceProcAddrException e)
            {
                Debug.LogWarning(e.Message);
                return false;
            }

            return
                xrCreateBodyTrackerFB != null &&
                xrDestroyBodyTrackerFB != null &&
                xrLocateBodyJointsFB != null &&
                xrGetBodySkeletonFB != null &&
                true;
        }

        protected override void UnhookFunctions()
        {
            intercept_xrGetSystem = null;
            intercept_xrWaitFrame = null;
            xrGetSystemProperties = null;
            xrCreateBodyTrackerFB = null;
            xrDestroyBodyTrackerFB = null;
            xrLocateBodyJointsFB = null;
            xrGetBodySkeletonFB = null;
        }
    }
#endregion
}


namespace UnityEngine.XR.OpenXR.NativeTypes
{
    // FB_body_tracking

    public enum XrBodyJointFB
    {
        XR_BODY_JOINT_ROOT_FB = 0,
        XR_BODY_JOINT_HIPS_FB = 1,
        XR_BODY_JOINT_SPINE_LOWER_FB = 2,
        XR_BODY_JOINT_SPINE_MIDDLE_FB = 3,
        XR_BODY_JOINT_SPINE_UPPER_FB = 4,
        XR_BODY_JOINT_CHEST_FB = 5,
        XR_BODY_JOINT_NECK_FB = 6,
        XR_BODY_JOINT_HEAD_FB = 7,
        XR_BODY_JOINT_LEFT_SHOULDER_FB = 8,
        XR_BODY_JOINT_LEFT_SCAPULA_FB = 9,
        XR_BODY_JOINT_LEFT_ARM_UPPER_FB = 10,
        XR_BODY_JOINT_LEFT_ARM_LOWER_FB = 11,
        XR_BODY_JOINT_LEFT_HAND_WRIST_TWIST_FB = 12,
        XR_BODY_JOINT_RIGHT_SHOULDER_FB = 13,
        XR_BODY_JOINT_RIGHT_SCAPULA_FB = 14,
        XR_BODY_JOINT_RIGHT_ARM_UPPER_FB = 15,
        XR_BODY_JOINT_RIGHT_ARM_LOWER_FB = 16,
        XR_BODY_JOINT_RIGHT_HAND_WRIST_TWIST_FB = 17,
        XR_BODY_JOINT_LEFT_HAND_PALM_FB = 18,
        XR_BODY_JOINT_LEFT_HAND_WRIST_FB = 19,
        XR_BODY_JOINT_LEFT_HAND_THUMB_METACARPAL_FB = 20,
        XR_BODY_JOINT_LEFT_HAND_THUMB_PROXIMAL_FB = 21,
        XR_BODY_JOINT_LEFT_HAND_THUMB_DISTAL_FB = 22,
        XR_BODY_JOINT_LEFT_HAND_THUMB_TIP_FB = 23,
        XR_BODY_JOINT_LEFT_HAND_INDEX_METACARPAL_FB = 24,
        XR_BODY_JOINT_LEFT_HAND_INDEX_PROXIMAL_FB = 25,
        XR_BODY_JOINT_LEFT_HAND_INDEX_INTERMEDIATE_FB = 26,
        XR_BODY_JOINT_LEFT_HAND_INDEX_DISTAL_FB = 27,
        XR_BODY_JOINT_LEFT_HAND_INDEX_TIP_FB = 28,
        XR_BODY_JOINT_LEFT_HAND_MIDDLE_METACARPAL_FB = 29,
        XR_BODY_JOINT_LEFT_HAND_MIDDLE_PROXIMAL_FB = 30,
        XR_BODY_JOINT_LEFT_HAND_MIDDLE_INTERMEDIATE_FB = 31,
        XR_BODY_JOINT_LEFT_HAND_MIDDLE_DISTAL_FB = 32,
        XR_BODY_JOINT_LEFT_HAND_MIDDLE_TIP_FB = 33,
        XR_BODY_JOINT_LEFT_HAND_RING_METACARPAL_FB = 34,
        XR_BODY_JOINT_LEFT_HAND_RING_PROXIMAL_FB = 35,
        XR_BODY_JOINT_LEFT_HAND_RING_INTERMEDIATE_FB = 36,
        XR_BODY_JOINT_LEFT_HAND_RING_DISTAL_FB = 37,
        XR_BODY_JOINT_LEFT_HAND_RING_TIP_FB = 38,
        XR_BODY_JOINT_LEFT_HAND_LITTLE_METACARPAL_FB = 39,
        XR_BODY_JOINT_LEFT_HAND_LITTLE_PROXIMAL_FB = 40,
        XR_BODY_JOINT_LEFT_HAND_LITTLE_INTERMEDIATE_FB = 41,
        XR_BODY_JOINT_LEFT_HAND_LITTLE_DISTAL_FB = 42,
        XR_BODY_JOINT_LEFT_HAND_LITTLE_TIP_FB = 43,
        XR_BODY_JOINT_RIGHT_HAND_PALM_FB = 44,
        XR_BODY_JOINT_RIGHT_HAND_WRIST_FB = 45,
        XR_BODY_JOINT_RIGHT_HAND_THUMB_METACARPAL_FB = 46,
        XR_BODY_JOINT_RIGHT_HAND_THUMB_PROXIMAL_FB = 47,
        XR_BODY_JOINT_RIGHT_HAND_THUMB_DISTAL_FB = 48,
        XR_BODY_JOINT_RIGHT_HAND_THUMB_TIP_FB = 49,
        XR_BODY_JOINT_RIGHT_HAND_INDEX_METACARPAL_FB = 50,
        XR_BODY_JOINT_RIGHT_HAND_INDEX_PROXIMAL_FB = 51,
        XR_BODY_JOINT_RIGHT_HAND_INDEX_INTERMEDIATE_FB = 52,
        XR_BODY_JOINT_RIGHT_HAND_INDEX_DISTAL_FB = 53,
        XR_BODY_JOINT_RIGHT_HAND_INDEX_TIP_FB = 54,
        XR_BODY_JOINT_RIGHT_HAND_MIDDLE_METACARPAL_FB = 55,
        XR_BODY_JOINT_RIGHT_HAND_MIDDLE_PROXIMAL_FB = 56,
        XR_BODY_JOINT_RIGHT_HAND_MIDDLE_INTERMEDIATE_FB = 57,
        XR_BODY_JOINT_RIGHT_HAND_MIDDLE_DISTAL_FB = 58,
        XR_BODY_JOINT_RIGHT_HAND_MIDDLE_TIP_FB = 59,
        XR_BODY_JOINT_RIGHT_HAND_RING_METACARPAL_FB = 60,
        XR_BODY_JOINT_RIGHT_HAND_RING_PROXIMAL_FB = 61,
        XR_BODY_JOINT_RIGHT_HAND_RING_INTERMEDIATE_FB = 62,
        XR_BODY_JOINT_RIGHT_HAND_RING_DISTAL_FB = 63,
        XR_BODY_JOINT_RIGHT_HAND_RING_TIP_FB = 64,
        XR_BODY_JOINT_RIGHT_HAND_LITTLE_METACARPAL_FB = 65,
        XR_BODY_JOINT_RIGHT_HAND_LITTLE_PROXIMAL_FB = 66,
        XR_BODY_JOINT_RIGHT_HAND_LITTLE_INTERMEDIATE_FB = 67,
        XR_BODY_JOINT_RIGHT_HAND_LITTLE_DISTAL_FB = 68,
        XR_BODY_JOINT_RIGHT_HAND_LITTLE_TIP_FB = 69,
        XR_BODY_JOINT_COUNT_FB = 70,
        XR_BODY_JOINT_NONE_FB = -1,
        XR_BODY_JOINT_MAX_ENUM_FB = 0x7FFFFFFF
    };

    public enum XrBodyJointSetFB
    {
        XR_BODY_JOINT_SET_DEFAULT_FB = 0,
        XR_BODY_JOINT_SET_FULL_BODY_META = 1000274000,
        XR_BODY_JOINT_SET_MAX_ENUM_FB = 0x7FFFFFFF
    };

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct XrSystemBodyTrackingPropertiesFB
    {
        public XrStructureType type;
        public void* next;
        public XrBool32 supportsBodyTracking;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct XrBodyTrackerCreateInfoFB
    {
        public XrStructureType type;
        public void* next;
        public XrBodyJointSetFB bodyJointSet;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct XrBodyJointsLocateInfoFB
    {
        public XrStructureType type;
        public void* next;
        public XrSpace baseSpace;
        public XrTime time;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct XrBodyJointLocationFB
    {
        public XrSpaceLocationFlagsExt locationFlags;
        public XrPosef pose;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct XrBodyJointLocationsFB
    {
        public XrStructureType type;
        public void* next;
        public XrBool32 isActive;
        public float confidence;
        public UInt32 jointCount;
        public XrBodyJointLocationFB* jointLocations;
        public UInt32 skeletonChangedCount;
        public XrTime time;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct XrBodySkeletonJointFB
    {
        public Int32 joint;
        public Int32 parentJoint;
        public XrPosef pose;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct XrBodySkeletonFB
    {
        public XrStructureType type;
        public void* next;
        public UInt32 jointCount;
        public XrBodySkeletonJointFB* joints;
    }

    unsafe delegate XrResult del_xrCreateBodyTrackerFB(UInt64 session, XrBodyTrackerCreateInfoFB* createInfo, out UInt64 bodyTracker);
    delegate XrResult del_xrDestroyBodyTrackerFB(UInt64 bodyTracker);
    unsafe delegate XrResult del_xrLocateBodyJointsFB(UInt64 bodyTracker, XrBodyJointsLocateInfoFB* locateInfo, XrBodyJointLocationsFB* locations);
    unsafe delegate XrResult del_xrGetBodySkeletonFB(UInt64 bodyTracker, XrBodySkeletonFB* skeleton);

    // META_body_tracking_full_body

    public enum XrFullBodyJointMETA
    {
        XR_FULL_BODY_JOINT_ROOT_META = 0,
        XR_FULL_BODY_JOINT_HIPS_META = 1,
        XR_FULL_BODY_JOINT_SPINE_LOWER_META = 2,
        XR_FULL_BODY_JOINT_SPINE_MIDDLE_META = 3,
        XR_FULL_BODY_JOINT_SPINE_UPPER_META = 4,
        XR_FULL_BODY_JOINT_CHEST_META = 5,
        XR_FULL_BODY_JOINT_NECK_META = 6,
        XR_FULL_BODY_JOINT_HEAD_META = 7,
        XR_FULL_BODY_JOINT_LEFT_SHOULDER_META = 8,
        XR_FULL_BODY_JOINT_LEFT_SCAPULA_META = 9,
        XR_FULL_BODY_JOINT_LEFT_ARM_UPPER_META = 10,
        XR_FULL_BODY_JOINT_LEFT_ARM_LOWER_META = 11,
        XR_FULL_BODY_JOINT_LEFT_HAND_WRIST_TWIST_META = 12,
        XR_FULL_BODY_JOINT_RIGHT_SHOULDER_META = 13,
        XR_FULL_BODY_JOINT_RIGHT_SCAPULA_META = 14,
        XR_FULL_BODY_JOINT_RIGHT_ARM_UPPER_META = 15,
        XR_FULL_BODY_JOINT_RIGHT_ARM_LOWER_META = 16,
        XR_FULL_BODY_JOINT_RIGHT_HAND_WRIST_TWIST_META = 17,
        XR_FULL_BODY_JOINT_LEFT_HAND_PALM_META = 18,
        XR_FULL_BODY_JOINT_LEFT_HAND_WRIST_META = 19,
        XR_FULL_BODY_JOINT_LEFT_HAND_THUMB_METACARPAL_META = 20,
        XR_FULL_BODY_JOINT_LEFT_HAND_THUMB_PROXIMAL_META = 21,
        XR_FULL_BODY_JOINT_LEFT_HAND_THUMB_DISTAL_META = 22,
        XR_FULL_BODY_JOINT_LEFT_HAND_THUMB_TIP_META = 23,
        XR_FULL_BODY_JOINT_LEFT_HAND_INDEX_METACARPAL_META = 24,
        XR_FULL_BODY_JOINT_LEFT_HAND_INDEX_PROXIMAL_META = 25,
        XR_FULL_BODY_JOINT_LEFT_HAND_INDEX_INTERMEDIATE_META = 26,
        XR_FULL_BODY_JOINT_LEFT_HAND_INDEX_DISTAL_META = 27,
        XR_FULL_BODY_JOINT_LEFT_HAND_INDEX_TIP_META = 28,
        XR_FULL_BODY_JOINT_LEFT_HAND_MIDDLE_METACARPAL_META = 29,
        XR_FULL_BODY_JOINT_LEFT_HAND_MIDDLE_PROXIMAL_META = 30,
        XR_FULL_BODY_JOINT_LEFT_HAND_MIDDLE_INTERMEDIATE_META = 31,
        XR_FULL_BODY_JOINT_LEFT_HAND_MIDDLE_DISTAL_META = 32,
        XR_FULL_BODY_JOINT_LEFT_HAND_MIDDLE_TIP_META = 33,
        XR_FULL_BODY_JOINT_LEFT_HAND_RING_METACARPAL_META = 34,
        XR_FULL_BODY_JOINT_LEFT_HAND_RING_PROXIMAL_META = 35,
        XR_FULL_BODY_JOINT_LEFT_HAND_RING_INTERMEDIATE_META = 36,
        XR_FULL_BODY_JOINT_LEFT_HAND_RING_DISTAL_META = 37,
        XR_FULL_BODY_JOINT_LEFT_HAND_RING_TIP_META = 38,
        XR_FULL_BODY_JOINT_LEFT_HAND_LITTLE_METACARPAL_META = 39,
        XR_FULL_BODY_JOINT_LEFT_HAND_LITTLE_PROXIMAL_META = 40,
        XR_FULL_BODY_JOINT_LEFT_HAND_LITTLE_INTERMEDIATE_META = 41,
        XR_FULL_BODY_JOINT_LEFT_HAND_LITTLE_DISTAL_META = 42,
        XR_FULL_BODY_JOINT_LEFT_HAND_LITTLE_TIP_META = 43,
        XR_FULL_BODY_JOINT_RIGHT_HAND_PALM_META = 44,
        XR_FULL_BODY_JOINT_RIGHT_HAND_WRIST_META = 45,
        XR_FULL_BODY_JOINT_RIGHT_HAND_THUMB_METACARPAL_META = 46,
        XR_FULL_BODY_JOINT_RIGHT_HAND_THUMB_PROXIMAL_META = 47,
        XR_FULL_BODY_JOINT_RIGHT_HAND_THUMB_DISTAL_META = 48,
        XR_FULL_BODY_JOINT_RIGHT_HAND_THUMB_TIP_META = 49,
        XR_FULL_BODY_JOINT_RIGHT_HAND_INDEX_METACARPAL_META = 50,
        XR_FULL_BODY_JOINT_RIGHT_HAND_INDEX_PROXIMAL_META = 51,
        XR_FULL_BODY_JOINT_RIGHT_HAND_INDEX_INTERMEDIATE_META = 52,
        XR_FULL_BODY_JOINT_RIGHT_HAND_INDEX_DISTAL_META = 53,
        XR_FULL_BODY_JOINT_RIGHT_HAND_INDEX_TIP_META = 54,
        XR_FULL_BODY_JOINT_RIGHT_HAND_MIDDLE_METACARPAL_META = 55,
        XR_FULL_BODY_JOINT_RIGHT_HAND_MIDDLE_PROXIMAL_META = 56,
        XR_FULL_BODY_JOINT_RIGHT_HAND_MIDDLE_INTERMEDIATE_META = 57,
        XR_FULL_BODY_JOINT_RIGHT_HAND_MIDDLE_DISTAL_META = 58,
        XR_FULL_BODY_JOINT_RIGHT_HAND_MIDDLE_TIP_META = 59,
        XR_FULL_BODY_JOINT_RIGHT_HAND_RING_METACARPAL_META = 60,
        XR_FULL_BODY_JOINT_RIGHT_HAND_RING_PROXIMAL_META = 61,
        XR_FULL_BODY_JOINT_RIGHT_HAND_RING_INTERMEDIATE_META = 62,
        XR_FULL_BODY_JOINT_RIGHT_HAND_RING_DISTAL_META = 63,
        XR_FULL_BODY_JOINT_RIGHT_HAND_RING_TIP_META = 64,
        XR_FULL_BODY_JOINT_RIGHT_HAND_LITTLE_METACARPAL_META = 65,
        XR_FULL_BODY_JOINT_RIGHT_HAND_LITTLE_PROXIMAL_META = 66,
        XR_FULL_BODY_JOINT_RIGHT_HAND_LITTLE_INTERMEDIATE_META = 67,
        XR_FULL_BODY_JOINT_RIGHT_HAND_LITTLE_DISTAL_META = 68,
        XR_FULL_BODY_JOINT_RIGHT_HAND_LITTLE_TIP_META = 69,
        XR_FULL_BODY_JOINT_LEFT_UPPER_LEG_META = 70,
        XR_FULL_BODY_JOINT_LEFT_LOWER_LEG_META = 71,
        XR_FULL_BODY_JOINT_LEFT_FOOT_ANKLE_TWIST_META = 72,
        XR_FULL_BODY_JOINT_LEFT_FOOT_ANKLE_META = 73,
        XR_FULL_BODY_JOINT_LEFT_FOOT_SUBTALAR_META = 74,
        XR_FULL_BODY_JOINT_LEFT_FOOT_TRANSVERSE_META = 75,
        XR_FULL_BODY_JOINT_LEFT_FOOT_BALL_META = 76,
        XR_FULL_BODY_JOINT_RIGHT_UPPER_LEG_META = 77,
        XR_FULL_BODY_JOINT_RIGHT_LOWER_LEG_META = 78,
        XR_FULL_BODY_JOINT_RIGHT_FOOT_ANKLE_TWIST_META = 79,
        XR_FULL_BODY_JOINT_RIGHT_FOOT_ANKLE_META = 80,
        XR_FULL_BODY_JOINT_RIGHT_FOOT_SUBTALAR_META = 81,
        XR_FULL_BODY_JOINT_RIGHT_FOOT_TRANSVERSE_META = 82,
        XR_FULL_BODY_JOINT_RIGHT_FOOT_BALL_META = 83,
        XR_FULL_BODY_JOINT_COUNT_META = 84,
        XR_FULL_BODY_JOINT_NONE_META = 85,
        XR_FULL_BODY_JOINT_MAX_ENUM_META = 0x7FFFFFFF
    };

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct  XrSystemPropertiesBodyTrackingFullBodyMETA
    {
        public XrStructureType type;
        public void* next;
        public XrBool32 supportsFullBodyTracking;
    }
}
