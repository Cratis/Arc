// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Queries;

/// <summary>
/// Captures the effective subscription scope as serialized data, independent of the filter's original value.
/// </summary>
internal sealed class ObservableQuerySubscriptionScopeSnapshot
{
    readonly Type _runtimeType;
    readonly byte[] _serializedValue;
    readonly JsonSerializerOptions _serializerOptions;

    /// <summary>
    /// Initializes an immutable subscription scope snapshot, rejecting values that cannot be round-tripped.
    /// </summary>
    /// <param name="scope">The non-null scope supplied by a filter.</param>
    /// <param name="serializerOptions">The Arc JSON serializer options.</param>
    /// <exception cref="InvalidSubscriptionScope">The supplied value cannot be represented.</exception>
    public ObservableQuerySubscriptionScopeSnapshot(object scope, JsonSerializerOptions serializerOptions)
    {
        _runtimeType = scope.GetType();
        _serializerOptions = serializerOptions;
        try
        {
            _serializedValue = Serialize(scope);
            var restored = Deserialize();
            if (restored is null || restored.GetType() != _runtimeType || !SerializesEquivalently(restored))
            {
                throw new InvalidSubscriptionScope(_runtimeType);
            }
        }
        catch (Exception error) when (error is JsonException or NotSupportedException or InvalidOperationException)
        {
            throw new InvalidSubscriptionScope(_runtimeType, error);
        }
    }

    /// <summary>
    /// Creates a fresh value from the subscription's immutable baseline.
    /// </summary>
    /// <returns>An independent copy of the scope.</returns>
    /// <exception cref="InvalidSubscriptionScope">The captured scope cannot be restored.</exception>
    public object CreateScope()
    {
        try
        {
            return Deserialize() ?? throw new InvalidSubscriptionScope(_runtimeType);
        }
        catch (Exception error) when (error is JsonException or NotSupportedException or InvalidOperationException)
        {
            throw new InvalidSubscriptionScope(_runtimeType, error);
        }
    }

    /// <summary>
    /// Gets whether the restored copy serializes to the same JSON as the captured snapshot.
    /// </summary>
    /// <param name="restored">The scope as restored from the snapshot.</param>
    /// <returns>True when both are equivalent JSON.</returns>
    /// <remarks>
    /// Compares against the captured bytes, which are what <see cref="CreateScope"/> restores from, and parses with the
    /// document options the serializer itself would parse with, so the comparison is the same as comparing the
    /// elements the serializer builds.
    /// </remarks>
    bool SerializesEquivalently(object restored)
    {
        var documentOptions = new JsonDocumentOptions { MaxDepth = _serializerOptions.MaxDepth };
        using var original = JsonDocument.Parse(_serializedValue, documentOptions);
        using var roundTripped = JsonDocument.Parse(Serialize(restored), documentOptions);
        return JsonElement.DeepEquals(original.RootElement, roundTripped.RootElement);
    }

    /// <summary>
    /// Serializes a scope value by the captured runtime type.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <returns>The UTF-8 JSON of the value.</returns>
    /// <remarks>
    /// This and <see cref="Deserialize"/> are the only places the snapshot goes through the runtime type. The Arc
    /// serializer options carry no type info resolver, so the type can only be resolved by reflection here until those
    /// options are backed by generated metadata.
    /// </remarks>
    byte[] Serialize(object value) => JsonSerializer.SerializeToUtf8Bytes(value, _runtimeType, _serializerOptions);

    /// <summary>
    /// Restores a scope value from the captured JSON.
    /// </summary>
    /// <returns>The restored value, or <see langword="null"/> when the JSON holds <see langword="null"/>.</returns>
    object? Deserialize() => JsonSerializer.Deserialize(_serializedValue, _runtimeType, _serializerOptions);
}
