// SPDX-FileCopyrightText: 2024 Michael Nisbet <me@mikesky.dev>
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.NativeTypes;

namespace OpenXR.Extensions
{
    internal static class GetInstanceProcAddrInterceptor
    {
        private static readonly del_xrGetInstanceProcAddr s_Callback = Intercepted_xrGetInstanceProcAddr;
        private static readonly List<del_xrGetInstanceProcAddr> s_Handlers = new List<del_xrGetInstanceProcAddr>();
        private static del_xrGetInstanceProcAddr s_GetInstanceProcAddr;
        private static IntPtr s_CallbackPointer;

        public static del_xrGetInstanceProcAddr GetInstanceProcAddr => s_GetInstanceProcAddr;

        public static IntPtr Hook(IntPtr xrGetInstanceProcAddr, del_xrGetInstanceProcAddr handler)
        {
            if (!s_Handlers.Contains(handler))
            {
                s_Handlers.Add(handler);
            }

            if (s_CallbackPointer == IntPtr.Zero)
            {
                s_CallbackPointer = Marshal.GetFunctionPointerForDelegate(s_Callback);
            }

            // Insert the shared callback only once per loader chain. A later feature may
            // receive another package's wrapper around our callback: rebinding our
            // downstream delegate to it would create a cycle. Returning our callback
            // again would instead discard that wrapper, so preserve the incoming chain.
            if (s_GetInstanceProcAddr != null)
            {
                return xrGetInstanceProcAddr;
            }

            // Unhook clears this binding when the final handler is removed.
            s_GetInstanceProcAddr = Marshal.GetDelegateForFunctionPointer<del_xrGetInstanceProcAddr>(
                xrGetInstanceProcAddr);

            return s_CallbackPointer;
        }

        public static void Unhook(del_xrGetInstanceProcAddr handler)
        {
            s_Handlers.Remove(handler);
            if (s_Handlers.Count == 0)
            {
                s_GetInstanceProcAddr = null;
            }
        }

        [MonoPInvokeCallback(typeof(del_xrGetInstanceProcAddr))]
        private static XrResult Intercepted_xrGetInstanceProcAddr(ulong instance, string originFunctionName,
            ref IntPtr originFunctionPointer)
        {
            if (s_GetInstanceProcAddr == null)
            {
                return (XrResult)0;
            }

            XrResult result = s_GetInstanceProcAddr(instance, originFunctionName, ref originFunctionPointer);
            del_xrGetInstanceProcAddr[] handlers = s_Handlers.ToArray();
            foreach (del_xrGetInstanceProcAddr handler in handlers)
            {
                handler.Invoke(instance, originFunctionName, ref originFunctionPointer);
            }
            return result;
        }
    }

    public abstract class FeatureBase<Feature> : OpenXRFeature where Feature : OpenXRFeature
    {
        protected static ulong XrInstance = 0;
        protected static ulong XrSession = 0;

        protected static del_xrGetInstanceProcAddr GetInstanceProcAddr;
        protected static del_xrGetInstanceProcAddr OnGetInstanceProcAddr;
        private static readonly del_xrGetInstanceProcAddr s_OnGetInstanceProcAddr = HandleGetInstanceProcAddr;

        public static bool FeatureEnabled => OpenXRSettings.Instance.GetFeature<Feature>().enabled;

        private static XrResult HandleGetInstanceProcAddr(ulong instance, string originFunctionName,
            ref IntPtr originFunctionPointer)
        {
            OnGetInstanceProcAddr?.Invoke(instance, originFunctionName, ref originFunctionPointer);
            return (XrResult)0;
        }

        protected static void InterceptFunction<T>(string functionNameToReplace, T replacementFunctionDelegate, ref T originFunctionDelegate, string originFunctionName, ref IntPtr originFunctionPointer)
        {
            if (originFunctionName != functionNameToReplace || originFunctionDelegate != null)
                return;

            // Assign origin function delegate
            originFunctionDelegate = Marshal.GetDelegateForFunctionPointer<T>(originFunctionPointer);

            // Set origin function pointer to the replacement delegate
            originFunctionPointer = Marshal.GetFunctionPointerForDelegate(replacementFunctionDelegate);
        }

        protected XrResult HookFunction<T>(string functionName, ref T functionDelegate)
        {
            if (functionDelegate != null)
            {
                Debug.LogWarning($"Function {functionName} was already hooked, overwriting.");
            }

            XrResult result;
            IntPtr functionPtr = IntPtr.Zero;

            result = GetInstanceProcAddr(XrInstance, functionName, ref functionPtr);
            if (result != 0)
            {
                throw new GetInstanceProcAddrException($"Failed to find {functionName}. Error code: {result}");
            }

            functionDelegate = Marshal.GetDelegateForFunctionPointer<T>(functionPtr);

            return result;
        }

        protected abstract bool CheckRequiredExtensions();

        protected virtual bool HookFunctions()
        {
            return true;
        }

        protected virtual void UnhookFunctions()
        {
        }


        #region OpenXRFeature Overrides
        protected override bool OnInstanceCreate(ulong xrInstance)
        {
            XrInstance = xrInstance;

            bool result = base.OnInstanceCreate(xrInstance);

            if (result)
                result = CheckRequiredExtensions();

            if (result)
                result = HookFunctions();

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
            GetInstanceProcAddr = null;
            OnGetInstanceProcAddr = null;
            GetInstanceProcAddrInterceptor.Unhook(s_OnGetInstanceProcAddr);
            UnhookFunctions();
        }

        protected override IntPtr HookGetInstanceProcAddr(IntPtr xrGetInstanceProcAddr)
        {
            IntPtr hookedGetInstanceProcAddr = GetInstanceProcAddrInterceptor.Hook(
                xrGetInstanceProcAddr, s_OnGetInstanceProcAddr);
            GetInstanceProcAddr = GetInstanceProcAddrInterceptor.GetInstanceProcAddr;
            return hookedGetInstanceProcAddr;
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
