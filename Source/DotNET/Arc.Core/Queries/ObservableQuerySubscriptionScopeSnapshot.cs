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
            _serializedValue = JsonSerializer.SerializeToUtf8Bytes(scope, _runtimeType, serializerOptions);
            var restored = JsonSerializer.Deserialize(_serializedValue, _runtimeType, serializerOptions);
            if (restored is null || restored.GetType() != _runtimeType ||
                !JsonElement.DeepEquals(
                    JsonSerializer.SerializeToElement(scope, _runtimeType, serializerOptions),
                    JsonSerializer.SerializeToElement(restored, _runtimeType, serializerOptions)))
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
            return JsonSerializer.Deserialize(_serializedValue, _runtimeType, _serializerOptions)
                ?? throw new InvalidSubscriptionScope(_runtimeType);
        }
        catch (Exception error) when (error is JsonException or NotSupportedException or InvalidOperationException)
        {
            throw new InvalidSubscriptionScope(_runtimeType, error);
        }
    }
}
