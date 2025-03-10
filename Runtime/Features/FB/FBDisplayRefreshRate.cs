// SPDX-FileCopyrightText: 2024 Michael Nisbet <me@mikesky.dev>
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.InteropServices;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine;
using AOT;
using UnityEngine.XR.OpenXR;


#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
#endif
using UnityEngine.XR.OpenXR.NativeTypes;

namespace OpenXR.Extensions
{
#if UNITY_EDITOR
    [OpenXRFeature(UiName = "FB Display Refresh Rate",
        BuildTargetGroups = new [] {BuildTargetGroup.Standalone, BuildTargetGroup.WSA, BuildTargetGroup.Android},
        Company = "Mikesky",
        Desc = "Implement XR_FB_display_refresh_rate",
        OpenxrExtensionStrings = XR_FB_DISPLAY_REFRESH_RATE,
        Version = "0.1.0",
        FeatureId = FeatureId,
        Priority = 1)]
#endif
    public class FBDisplayRefreshRate : FeatureBase<FBDisplayRefreshRate>
    {
        public const string XR_FB_DISPLAY_REFRESH_RATE = "XR_FB_display_refresh_rate";
        public const string FeatureId = "dev.mikesky.openxr.extensions.fbdisplayrefreshrate";

        public static float DisplayRefreshRate
        {
            get { return GetRefreshRate(); }
            set { SetRefreshRate(value); }
        }

        public static float[] GetDisplayRefreshRates()
        {
            if (!FeatureEnabled || xrEnumerateDisplayRefreshRatesFB == null)
            {
                return null;
            }

            unsafe
            {
                UInt32 displayRefreshRateCapacity;
                var result = xrEnumerateDisplayRefreshRatesFB(XrSession, 0, &displayRefreshRateCapacity, null);
                if (result == XrResult.Success)
                {
                    float[] refreshRates = new float[displayRefreshRateCapacity];
                    fixed (float* displayRates = &refreshRates[0])
                    {
                        result = xrEnumerateDisplayRefreshRatesFB(XrSession, displayRefreshRateCapacity, &displayRefreshRateCapacity, displayRates);
                        if (result == XrResult.Success)
                        {
                            return refreshRates;
                        }
                    }
                }
                else
                {
                    Debug.LogWarning(result);
                }
            }

            return null;
        }

        private static float GetRefreshRate()
        {
            if (!FeatureEnabled || xrGetDisplayRefreshRateFB == null)
            {
                return 0.0f;
            }

            var success = xrGetDisplayRefreshRateFB(XrSession, out var displayRefreshRate);
            if (success == XrResult.Success)
            {
                return displayRefreshRate;
            }
            return 0.0f;
        }

        private static bool SetRefreshRate(float targetRefreshRate)
        {
            if (!FeatureEnabled || xrRequestDisplayRefreshRateFB == null)
            {
                return false;
            }

            float[] rates = GetDisplayRefreshRates();

            if (rates == null || rates.Length == 0)
            {
                return false;
            }

            var success = xrRequestDisplayRefreshRateFB(XrSession, targetRefreshRate);
            if (success == XrResult.Success)
            {
                return true;
            }

            return false;
        }

        public delegate void DisplayRefreshRateChanged(float fromDisplayRefreshRate, float toDisplayRefreshRate);
        public static DisplayRefreshRateChanged OnDisplayRefreshRateChanged;

        #region OpenXR native bindings
        static del_xrEnumerateDisplayRefreshRatesFB xrEnumerateDisplayRefreshRatesFB;
        static del_xrGetDisplayRefreshRateFB xrGetDisplayRefreshRateFB;
        static del_xrRequestDisplayRefreshRateFB xrRequestDisplayRefreshRateFB;
        static del_xrPollEvent xrPollEvent;

        [MonoPInvokeCallback(typeof(del_xrPollEvent))]
        private static unsafe XrResult Intercepted_xrPollEvent(ulong xrSession, XrEventDataBuffer* eventData)
        {
            var result = xrPollEvent(xrSession, eventData);

            switch (eventData->type)
            {
                case XrStructureTypeExt.XR_TYPE_EVENT_DATA_DISPLAY_REFRESH_RATE_CHANGED_FB:
                    var refreshRateChangedEvent = (XrEventDataDisplayRefreshRateChangedFB*)eventData;
                    OnDisplayRefreshRateChanged?.Invoke(refreshRateChangedEvent->fromDisplayRefreshRate, refreshRateChangedEvent->toDisplayRefreshRate);
                    break;
            }

            return result;
        }

        protected unsafe override IntPtr HookGetInstanceProcAddr(IntPtr xrGetInstanceProcAddr)
        {
            OnGetInstanceProcAddr += Intercept;
            return base.HookGetInstanceProcAddr(xrGetInstanceProcAddr);
        }

        protected static unsafe XrResult Intercept(ulong instance, string requestedFunctionName, ref IntPtr outgoingFunctionPointer)
        {
            InterceptFunction("xrPollEvent", Intercepted_xrPollEvent, ref xrPollEvent, requestedFunctionName, ref outgoingFunctionPointer);
            return XrResult.Success;
        }

        protected override bool CheckRequiredExtensions()
        {
            return OpenXRRuntime.IsExtensionEnabled(XR_FB_DISPLAY_REFRESH_RATE);
        }

        protected unsafe override bool HookFunctions()
        {
            try
            {
                HookFunction("xrEnumerateDisplayRefreshRatesFB", ref xrEnumerateDisplayRefreshRatesFB);
                HookFunction("xrGetDisplayRefreshRateFB", ref xrGetDisplayRefreshRateFB);
                HookFunction("xrRequestDisplayRefreshRateFB", ref xrRequestDisplayRefreshRateFB);
            }
            catch (GetInstanceProcAddrException e)
            {
                Debug.LogWarning(e.Message);
                return false;
            }

            return
                xrEnumerateDisplayRefreshRatesFB != null &&
                xrGetDisplayRefreshRateFB != null &&
                xrRequestDisplayRefreshRateFB != null &&
                true;
        }

        protected override void UnhookFunctions()
        {
            xrPollEvent = null;
            xrEnumerateDisplayRefreshRatesFB = null;
            xrGetDisplayRefreshRateFB = null;
            xrRequestDisplayRefreshRateFB = null;
        }

        #endregion
    }
}


namespace UnityEngine.XR.OpenXR.NativeTypes
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct XrEventDataDisplayRefreshRateChangedFB
    {
        public XrStructureType type;
        public void* next;
        public float fromDisplayRefreshRate;
        public float toDisplayRefreshRate;
    }

    unsafe delegate XrResult del_xrEnumerateDisplayRefreshRatesFB(UInt64 session, UInt32 displayRefreshRateCapacityInput, UInt32* displayRefreshRateCapacityOutput, float* displayRefreshRates);
    delegate XrResult del_xrGetDisplayRefreshRateFB(UInt64 session, out float displayRefreshRate);
    delegate XrResult del_xrRequestDisplayRefreshRateFB(UInt64 session, float displayRefreshRate);
}
