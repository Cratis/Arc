// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Runtime.CompilerServices;

/// <summary>
/// Identifies the call site replaced by a generated interceptor.
/// </summary>
/// <param name="version">The location format version.</param>
/// <param name="data">The encoded source location.</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class InterceptsLocationAttribute(int version, string data) : Attribute
{
    /// <summary>
    /// Gets the format version.
    /// </summary>
    public int Version { get; } = version;

    /// <summary>
    /// Gets the encoded location.
    /// </summary>
    public string Data { get; } = data;
}
