// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Represents an <see cref="ICanResolveReadModelForCommand"/> that resolves read models backed by Chronicle through
/// <see cref="IReadModels"/>, keyed by the command's resolved key (the event source id).
/// </summary>
/// <param name="readModelTypes">The read model types Chronicle can resolve by key.</param>
public class ChronicleReadModelForCommandResolver(IEnumerable<Type> readModelTypes) : ICanResolveReadModelForCommand
{
    /// <inheritdoc/>
    public IEnumerable<Type> ReadModelTypes { get; } = readModelTypes;

    /// <inheritdoc/>
    public Task<object?> Resolve(Type readModelType, CommandContext commandContext)
    {
        var readModels = commandContext.ServiceProvider!.GetRequiredService<IReadModels>();
        return Task.FromResult(ReadModelServiceCollectionExtensions.ResolveReadModel(readModelType, commandContext, readModels));
    }
}
