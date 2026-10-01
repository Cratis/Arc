// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Catalog;

/// <summary>
/// Represents the kind of node an embedded Screenplay document describes.
/// </summary>
/// <remarks>
/// The kinds mirror the levels the generator emits documents for; the explorer renders them as a tree
/// through the parent relationship each catalog entry declares.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<EventModelDocumentKind>))]
public enum EventModelDocumentKind
{
    /// <summary>
    /// The document holds everything an assembly declares.
    /// </summary>
    [JsonStringEnumMemberName("assembly")]
    Assembly = 0,

    /// <summary>
    /// The document holds a single module.
    /// </summary>
    [JsonStringEnumMemberName("module")]
    Module = 1,

    /// <summary>
    /// The document holds a single feature.
    /// </summary>
    [JsonStringEnumMemberName("feature")]
    Feature = 2
}
