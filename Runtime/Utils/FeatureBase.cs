// SPDX-FileCopyrightText: 2024 Michael Nisbet <me@mikesky.dev>
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.NativeTypes;

namespace OpenXR.Extensions
{
    public abstract class FeatureBase<F> : OpenXRFeature where F : OpenXRFeature
    {
        protected static ulong XrInstance = 0;
        protected static ulong XrSession = 0;

        protected static del_xrGetInstanceProcAddr GetInstanceProcAddr;
        protected static del_xrGetInstanceProcAddr OnGetInstanceProcAddr;

        public static bool FeatureEnabled => OpenXRSettings.Instance.GetFeature<F>().enabled;

        [MonoPInvokeCallback(typeof(del_xrGetInstanceProcAddr))]
        protected static unsafe XrResult Intercepted_xrGetInstanceProcAddr(ulong instance, string originFunctionName, IntPtr* originFunctionPointer)
        {
            var result = GetInstanceProcAddr(instance, originFunctionName, originFunctionPointer);
            OnGetInstanceProcAddr?.Invoke(instance, originFunctionName, originFunctionPointer);
            return result;
        }

        // This doesn't work without a pointer.
        protected unsafe static void InterceptFunction<T>(string functionNameToReplace, T replacementFunctionDelegate, ref T originFunctionDelegate, string originFunctionName, IntPtr* originFunctionPointer)
        {
            if (originFunctionName != functionNameToReplace || originFunctionDelegate != null)
                return;

            // Assign origin function delegate
            originFunctionDelegate = Marshal.GetDelegateForFunctionPointer<T>(*originFunctionPointer);

            // Set origin function pointer to the replacement delegate
            *originFunctionPointer = Marshal.GetFunctionPointerForDelegate(replacementFunctionDelegate);
        }

        protected XrResult HookFunction<T>(string functionName, ref T functionDelegate)
        {
            if (functionDelegate != null)
            {
                Debug.LogWarning($"Function {functionName} already hooked. Ignoring.");
                return XrResult.ValidationFailure;
            }

            XrResult result;
            IntPtr functionPtr = IntPtr.Zero;
            unsafe
            {
                result = GetInstanceProcAddr(XrInstance, functionName, &functionPtr);
                if (result != 0)
                {
                    throw new GetInstanceProcAddrException($"Failed to find {functionName}. Error code: {result}");
                }
            }

            functionDelegate = Marshal.GetDelegateForFunctionPointer<T>(functionPtr);

            return result;
        }

        protected abstract bool CheckRequiredExtensions();

        protected virtual bool LoadBindings()
        {
            return true;
        }


        #region OpenXRFeature Overrides
        protected override bool OnInstanceCreate(ulong xrInstance)
        {
            XrInstance = xrInstance;

            bool result = base.OnInstanceCreate(xrInstance);

            if (result)
                result = CheckRequiredExtensions();

            if (result)
                result = LoadBindings();

            return result;
        }

        protected override void OnSessionCreate(ulong xrSession)
        {
            XrSession = xrSession;
        }

        protected override void OnSessionDestroy(ulong xrSession)
        {
            XrSession = 0;
        }

        protected override void OnInstanceDestroy(ulong xrInstance)
        {
            XrInstance = 0;
        }

        protected unsafe override IntPtr HookGetInstanceProcAddr(IntPtr xrGetInstanceProcAddr)
        {
            InterceptFunction("xrGetInstanceProcAddr", Intercepted_xrGetInstanceProcAddr, ref GetInstanceProcAddr, "xrGetInstanceProcAddr", &xrGetInstanceProcAddr);
            return xrGetInstanceProcAddr;
        }
        #endregion
    }

    public class GetInstanceProcAddrException : Exception
    {
        public GetInstanceProcAddrException(string message) : base(message)
        {
        }
    }
}
