// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Represents whether a document was authored or derived from C# analysis.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<EventModelOrigin>))]
public enum EventModelOrigin
{
    /// <summary>
    /// The document was authored and requires complete validation.
    /// </summary>
    [JsonStringEnumMemberName("embedded")]
    Embedded = 0,

    /// <summary>
    /// The document was derived from C# analysis and may have incomplete projection coverage.
    /// </summary>
    [JsonStringEnumMemberName("generated")]
    Generated = 1
}
