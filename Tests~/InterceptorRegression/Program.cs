using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenXR.Extensions;
using UnityEngine.XR.OpenXR.NativeTypes;

internal static class Program
{
    private static readonly List<string> Calls = new List<string>();
    private static readonly List<del_xrGetInstanceProcAddr> Roots = new List<del_xrGetInstanceProcAddr>();
    private static readonly del_xrGetInstanceProcAddr HandlerA = HandleA;
    private static readonly del_xrGetInstanceProcAddr HandlerB = HandleB;
    private static readonly del_xrGetInstanceProcAddr HandlerC = HandleC;
    private static void ResetState()
    {
        GetInstanceProcAddrInterceptor.Unhook(HandlerA);
        GetInstanceProcAddrInterceptor.Unhook(HandlerB);
        GetInstanceProcAddrInterceptor.Unhook(HandlerC);
    }

    private static int Main()
    {
        var tests = new (string Name, Action Run)[]
        {
            ("First registration forwards result and function pointer", FirstRegistration),
            ("Adjacent registrations invoke each handler once", Adjacent),
            ("Duplicate registration does not duplicate handler", Duplicate),
            ("Interleaved wrapper is preserved without recursion", Interleaved),
            ("Multiple interleaved wrappers are preserved", MultipleWrappers),
            ("Partial teardown keeps remaining handlers", PartialTeardown),
            ("Full teardown permits a new loader binding", Restart)
        };
        int failures = 0;
        foreach (var test in tests)
        {
            ResetState();
            Calls.Clear();
            Roots.Clear();
            try
            {
                test.Run();
                Console.WriteLine($"PASS {test.Name}");
            }
            catch (Exception error)
            {
                failures++;
                Console.WriteLine($"FAIL {test.Name}: {error.Message}");
            }
        }
        ResetState();
        Console.WriteLine($"{tests.Length - failures}/{tests.Length} passed");
        return failures == 0 ? 0 : 1;
    }

    private static IntPtr Pointer(del_xrGetInstanceProcAddr callback)
    {
        Roots.Add(callback);
        return Marshal.GetFunctionPointerForDelegate(callback);
    }

    private static IntPtr Origin(string label = "origin") => Pointer(
        (ulong instance, string name, ref IntPtr function) =>
        {
            if (instance != 42 || name != "xrTest") throw new Exception("Lookup arguments changed");
            Calls.Add(label);
            function = new IntPtr(123);
            return (XrResult)(-7);
        });

    private static IntPtr Wrap(IntPtr downstream, string label)
    {
        var next = Marshal.GetDelegateForFunctionPointer<del_xrGetInstanceProcAddr>(downstream);
        int depth = 0;
        return Pointer((ulong instance, string name, ref IntPtr function) =>
        {
            // Bound recursion so a regression fails instead of overflowing the stack.
            if (++depth > 1)
            {
                depth--;
                return (XrResult)(-99);
            }
            try
            {
                Calls.Add(label);
                return next(instance, name, ref function);
            }
            finally { depth--; }
        });
    }

    private static void Lookup(IntPtr pointer, params string[] expected)
    {
        Calls.Clear();
        var callback = Marshal.GetDelegateForFunctionPointer<del_xrGetInstanceProcAddr>(pointer);
        IntPtr function = IntPtr.Zero;
        XrResult result = callback(42, "xrTest", ref function);
        Equal(-7, (int)result, "Result must propagate unchanged");
        Equal(new IntPtr(123), function, "Function pointer must propagate unchanged");
        Equal(string.Join(",", expected), string.Join(",", Calls), "Call sequence");
    }

    private static void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception($"{message}: expected {expected}; actual {actual}");
    }

    private static XrResult HandleA(ulong instance, string name, ref IntPtr function)
    { Calls.Add("A"); return 0; }
    private static XrResult HandleB(ulong instance, string name, ref IntPtr function)
    { Calls.Add("B"); return 0; }
    private static XrResult HandleC(ulong instance, string name, ref IntPtr function)
    { Calls.Add("C"); return 0; }

    private static void FirstRegistration()
    {
        Lookup(GetInstanceProcAddrInterceptor.Hook(Origin(), HandlerA), "origin", "A");
    }
    private static void Adjacent()
    {
        IntPtr first = GetInstanceProcAddrInterceptor.Hook(Origin(), HandlerA);
        IntPtr second = GetInstanceProcAddrInterceptor.Hook(first, HandlerB);
        Equal(first, second, "Shared callback identity");
        Lookup(second, "origin", "A", "B");
    }
    private static void Duplicate()
    {
        IntPtr chain = GetInstanceProcAddrInterceptor.Hook(Origin(), HandlerA);
        chain = GetInstanceProcAddrInterceptor.Hook(chain, HandlerA);
        Lookup(chain, "origin", "A");
    }
    private static void Interleaved()
    {
        IntPtr chain = GetInstanceProcAddrInterceptor.Hook(Origin(), HandlerA);
        IntPtr wrapper = Wrap(chain, "wrapper");
        chain = GetInstanceProcAddrInterceptor.Hook(wrapper, HandlerB);
        Lookup(chain, "wrapper", "origin", "A", "B");
        Equal(wrapper, chain, "Incoming wrapper must remain the chain head");
    }
    private static void MultipleWrappers()
    {
        IntPtr chain = Wrap(Origin(), "inner");
        chain = GetInstanceProcAddrInterceptor.Hook(chain, HandlerA);
        chain = Wrap(chain, "middle");
        chain = GetInstanceProcAddrInterceptor.Hook(chain, HandlerB);
        chain = Wrap(chain, "outer");
        chain = GetInstanceProcAddrInterceptor.Hook(chain, HandlerC);
        Lookup(chain, "outer", "middle", "inner", "origin", "A", "B", "C");
    }
    private static void PartialTeardown()
    {
        IntPtr chain = GetInstanceProcAddrInterceptor.Hook(Origin(), HandlerA);
        chain = GetInstanceProcAddrInterceptor.Hook(chain, HandlerB);
        GetInstanceProcAddrInterceptor.Unhook(HandlerA);
        Lookup(chain, "origin", "B");
    }
    private static void Restart()
    {
        Adjacent();
        GetInstanceProcAddrInterceptor.Unhook(HandlerA);
        GetInstanceProcAddrInterceptor.Unhook(HandlerB);
        Equal<del_xrGetInstanceProcAddr>(null, GetInstanceProcAddrInterceptor.GetInstanceProcAddr,
            "Final teardown must release loader binding");
        Lookup(GetInstanceProcAddrInterceptor.Hook(Origin("new origin"), HandlerC), "new origin", "C");
    }
}
