// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Sinks;

/// <summary>
/// Declares the named sink a read model type is stored in, so Arc reads it from that sink's storage instead of the default.
/// </summary>
/// <remarks>
/// A storage integration such as MongoDB maps the name to its own named configuration. Applications on Chronicle can use
/// Chronicle's own <c>[Sink("name")]</c> instead; Arc reads both. When a type carries this attribute, its name wins.
/// </remarks>
/// <param name="name">The name of the sink.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public sealed class ReadModelSinkAttribute(string name) : Attribute
{
    /// <summary>
    /// Gets the name of the sink.
    /// </summary>
    public string Name { get; } = name;
}
