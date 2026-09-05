// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Queries.for_ReadModelForCommandServiceCollectionExtensions;

/// <summary>
/// A test <see cref="ICanResolveReadModelForCommand"/> that resolves pinned instances for the read model types it owns.
/// </summary>
/// <param name="readModelTypes">The read model types this resolver owns.</param>
/// <param name="instances">The pinned instances to resolve, keyed by read model type; a type with no entry resolves to null.</param>
/// <param name="ownership">How strongly this resolver claims its read model types; declared unless stated otherwise.</param>
public class a_read_model_resolver(
    IEnumerable<Type> readModelTypes,
    IReadOnlyDictionary<Type, object?>? instances = null,
    ReadModelForCommandOwnership ownership = ReadModelForCommandOwnership.Declared) : ICanResolveReadModelForCommand
{
    readonly IReadOnlyDictionary<Type, object?> _instances = instances ?? new Dictionary<Type, object?>();

    public IEnumerable<Type> ReadModelTypes { get; } = [.. readModelTypes];

    public ReadModelForCommandOwnership Ownership { get; } = ownership;

    public Task<object?> Resolve(Type readModelType, CommandContext commandContext) =>
        Task.FromResult(_instances.TryGetValue(readModelType, out var instance) ? instance : null);
}
