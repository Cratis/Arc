// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Arc.EntityFrameworkCore.Json.for_JsonConversion;

#pragma warning disable SA1402, SA1649 // Single type per file, File name should match first type name

public record PersonName(string FirstName, string LastName);
public record Address(string Street, string City);
public record PhoneNumber(string Number);

public class EntityWithJsonProperties
{
    public int Id { get; set; }

    [Json]
    public PersonName Name { get; set; } = null!;

    [Json]
    public Address Address { get; set; } = null!;

    public string Email { get; set; } = string.Empty;
}

public class EntityWithoutJsonProperties
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class EntityWithJsonConstructorParameters
{
    public int Id { get; set; }

    [Json]
    public PersonName Name { get; set; } = null!;

    public string Email { get; set; } = string.Empty;
}

public class EntityWithMixedJsonUsage
{
    public int Id { get; set; }

    [Json]
    public Address Address { get; set; } = null!;

    public PhoneNumber Phone { get; set; } = null!;
}

/// <summary>Types used to verify custom-converter support (interface scenario from the issue).</summary>
public interface IMyRequest
{
    int Count { get; }
}

public sealed record DoThing(int Count) : IMyRequest;

public class EntityWithInterfaceJsonProperty
{
    public int Id { get; set; }

    [Json]
    public IMyRequest Request { get; set; } = null!;
}

public class MyRequestJsonConverter : JsonConverter<IMyRequest>
{
    public override IMyRequest? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var count = doc.RootElement.GetProperty("count").GetInt32();
        return new DoThing(count);
    }

    public override void Write(Utf8JsonWriter writer, IMyRequest value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("count", value.Count);
        writer.WriteEndObject();
    }
}
