// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Company.Library;

/// <summary>
/// Call sites intercepted by the fixture's source generator.
/// </summary>
public static class CompilerFeatureCalls
{
    /// <summary>
    /// Calls the methods whose implementations are replaced at compilation.
    /// </summary>
    /// <returns>The generated values, including the feature seen by the generator driver.</returns>
    public static string Intercepted() => $"{CompilerFeatureCalls.Current()}|{CompilerFeatureCalls.Legacy()}";

    /// <summary>
    /// Provides the call enabled by InterceptorsNamespaces.
    /// </summary>
    /// <returns>The unintercepted value.</returns>
    public static string Current() => "not intercepted";

    /// <summary>
    /// Provides the call enabled by InterceptorsPreviewNamespaces.
    /// </summary>
    /// <returns>The unintercepted value.</returns>
    public static string Legacy() => "not intercepted";
}
