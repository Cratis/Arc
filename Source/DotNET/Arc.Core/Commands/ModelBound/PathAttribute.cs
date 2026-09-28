// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.ModelBound;

/// <summary>
/// Specifies a custom HTTP path for a model-bound command and its validation endpoint.
/// </summary>
/// <param name="path">The custom path, without an automatically prepended API prefix.</param>
[AttributeUsage(AttributeTargets.Class)]
public sealed class PathAttribute(string path) : Attribute
{
    /// <summary>
    /// Gets the custom path.
    /// </summary>
    public string Path { get; } = path;
}
