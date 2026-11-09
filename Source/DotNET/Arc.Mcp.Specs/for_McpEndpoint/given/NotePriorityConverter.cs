// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Arc.Mcp.for_McpEndpoint.given;

public class NotePriorityConverter : JsonConverter<NotePriority>
{
    public override NotePriority Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.GetString() switch
    {
        "urgent" => NotePriority.VeryHigh,
        "deferred" => NotePriority.VeryLow,
        _ => throw new JsonException()
    };

    public override void Write(Utf8JsonWriter writer, NotePriority value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value == NotePriority.VeryHigh ? "urgent" : "deferred");
}
