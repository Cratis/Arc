// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

/// <summary>
/// Shared fixture that caches expensive JavaScript resources.
/// The TypeScript compiler file (~3MB) is loaded once and reused across all tests.
/// </summary>
public sealed class SharedJavaScriptRuntimeFixture
{
    static SharedJavaScriptRuntimeFixture()
    {
        // Pre-load and cache the TypeScript compiler on first access
        // This avoids reading the 3MB file for every test
        TypeScriptCompilerCode = JavaScriptResources.GetTypeScriptCompiler();
        ArcBootstrapCode = JavaScriptResources.GetArcBootstrap();
    }

    /// <summary>
    /// Gets the cached TypeScript compiler code.
    /// </summary>
    public static string TypeScriptCompilerCode { get; }

    /// <summary>
    /// Gets the cached Arc bootstrap code.
    /// </summary>
    public static string ArcBootstrapCode { get; }
}
