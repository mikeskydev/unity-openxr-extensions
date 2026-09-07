// Compile-time substitutes, not a simulation of Unity, native OpenXR, or IL2CPP.
using System;

namespace AOT
{
    public sealed class MonoPInvokeCallbackAttribute : Attribute
    {
        public MonoPInvokeCallbackAttribute(Type type) { }
    }
}
namespace UnityEngine
{
    public enum RuntimeInitializeLoadType { SubsystemRegistration }
    public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute
    {
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type) { }
    }
    public static class Debug
    {
        public static void LogWarning(string message) { }
    }
}
namespace UnityEngine.XR.OpenXR
{
    public class OpenXRSettings
    {
        public static OpenXRSettings Instance { get; } = new OpenXRSettings();
        public T GetFeature<T>() where T : Features.OpenXRFeature => default;
    }
}
namespace UnityEngine.XR.OpenXR.Features
{
    public class OpenXRFeature
    {
        public bool enabled;
        protected virtual bool OnInstanceCreate(ulong instance) => true;
        protected virtual void OnInstanceDestroy(ulong instance) { }
        protected virtual void OnSessionCreate(ulong session) { }
        protected virtual void OnSessionDestroy(ulong session) { }
        protected virtual IntPtr HookGetInstanceProcAddr(IntPtr pointer) => pointer;
    }
}
namespace UnityEngine.XR.OpenXR.NativeTypes
{
    public enum XrResult { Success = 0 }
    public delegate XrResult del_xrGetInstanceProcAddr(ulong instance, string name, ref IntPtr function);
}
