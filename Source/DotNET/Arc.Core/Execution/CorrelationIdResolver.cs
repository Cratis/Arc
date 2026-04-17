// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Execution;

/// <summary>
/// Resolves and parses correlation IDs from transport values and the current execution context.
/// </summary>
public static class CorrelationIdResolver
{
    /// <summary>
    /// Tries to create a <see cref="CorrelationId"/> from the provided value.
    /// </summary>
    /// <param name="value">The value that might represent a correlation ID.</param>
    /// <param name="correlationId">The parsed <see cref="CorrelationId"/> when successful.</param>
    /// <returns><see langword="true"/> if the value could be converted to a <see cref="CorrelationId"/>; otherwise <see langword="false"/>.</returns>
    public static bool TryGet(object? value, out CorrelationId correlationId)
    {
        if (value is CorrelationId existingCorrelationId && existingCorrelationId != CorrelationId.NotSet)
        {
            correlationId = existingCorrelationId;
            return true;
        }

        if (value is Guid guid && guid != Guid.Empty)
        {
            correlationId = new CorrelationId(guid);
            return true;
        }

        if (Guid.TryParse(value?.ToString(), out var parsedGuid) && parsedGuid != Guid.Empty)
        {
            correlationId = new CorrelationId(parsedGuid);
            return true;
        }

        correlationId = CorrelationId.NotSet;
        return false;
    }

    /// <summary>
    /// Resolves a <see cref="CorrelationId"/> from a transport value or from the current execution context.
    /// </summary>
    /// <param name="value">The value that might represent a correlation ID.</param>
    /// <param name="correlationIdAccessor">The accessor for the current correlation ID.</param>
    /// <returns>The resolved <see cref="CorrelationId"/>.</returns>
    public static CorrelationId ResolveOrCreate(object? value, ICorrelationIdAccessor correlationIdAccessor)
    {
        if (TryGet(value, out var correlationId))
        {
            return correlationId;
        }

        return correlationIdAccessor.Current != CorrelationId.NotSet
            ? correlationIdAccessor.Current
            : CorrelationId.New();
    }
}
