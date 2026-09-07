# Shared OpenXR interceptor regression tests

Run with the .NET 8 SDK or newer from the package root:

```sh
dotnet run --project Tests~/InterceptorRegression
```

This dependency-free harness compiles the actual `Runtime/Utils/FeatureBase.cs`
with minimal Unity type substitutes. It exercises delegate/function-pointer
round trips, interleaved third-party wrappers, handler registration, result and
pointer propagation, partial teardown, and complete teardown/rebinding. A bounded wrapper
detects recursion without overflowing the process stack.

Unity ignores the `Tests~` directory. No Unity project or player build is required.
These are managed regression tests, not validation of IL2CPP code generation,
Unity lifecycle timing, or device behavior. The production callback remains a
rooted static, non-generic method with its existing AOT callback attribute.

The shared interceptor groups package handlers at its first insertion point;
it does not give each feature its own independently ordered native hook. These
tests verify chain preservation, not per-feature ordering relative to third-party
wrappers.
