// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Arc.Screenplay.Embedded.Hosting;

/// <summary>
/// The serialization the explorer answers with.
/// </summary>
/// <remarks>
/// The explorer serializes on its own terms rather than the host's: the board reads a document with
/// camel-cased members and numeric enums, and a host is free to configure its own API differently.
/// </remarks>
public static class EventModelJson
{
    /// <summary>
    /// Gets the options every response from the explorer is serialized with.
    /// </summary>
    public static JsonSerializerOptions SerializerOptions { get; } = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };
}
