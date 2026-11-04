// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Cratis.Chronicle.Testing.Events;

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// Materializes read models in command scenarios by folding events through Chronicle's own reducer or projection engine,
/// borrowing an existing testing store instead of building a new one for every read.
/// </summary>
/// <remarks>
/// The fold delegate is built once per read model type and kept weakly, so collectible test assemblies still unload. A
/// borrowed read model scenario owns its input and is disposed after the read, which releases everything it created
/// without touching the borrowed store.
/// </remarks>
internal static class ReadModelFolds
{
    static readonly MethodInfo _foldMethod = typeof(ReadModelFolds).GetMethod(nameof(Fold), BindingFlags.NonPublic | BindingFlags.Static)!;
    static readonly ConditionalWeakTable<Type, Func<EventStoreForTesting, ReadModelFoldInput, Task<object?>>> _folds = new();

    /// <summary>
    /// Folds the input through the read model's reducer or projection.
    /// </summary>
    /// <param name="readModelType">The read model type to materialize.</param>
    /// <param name="store">The store to borrow for the fold.</param>
    /// <param name="input">The events to fold.</param>
    /// <returns>The materialized instance, or <see langword="null"/> when none of the events concern the read model.</returns>
    public static Task<object?> Materialize(Type readModelType, EventStoreForTesting store, ReadModelFoldInput input) =>
        _folds.GetValue(readModelType, CreateFold)(store, input);

    static Func<EventStoreForTesting, ReadModelFoldInput, Task<object?>> CreateFold(Type readModelType) =>
        _foldMethod.MakeGenericMethod(readModelType).CreateDelegate<Func<EventStoreForTesting, ReadModelFoldInput, Task<object?>>>();

    static async Task<object?> Fold<TReadModel>(EventStoreForTesting store, ReadModelFoldInput input)
        where TReadModel : class
    {
        // No service provider: reducers and projections are activated exactly as a stand-alone ReadModelScenario does.
        // No initial state either: production only has a read model once an event it handles has happened, so the
        // instance stays null when none of the events concern it. Instance is read directly rather than through the
        // scenario read models facade, which throws for a read model that does not exist. The fold runs when Instance
        // is read, so the scenario is read before it is disposed.
        using var scenario = store.CreateReadModelScenario<TReadModel>();
        await (input switch
        {
            ReadModelFoldInput.Appended appended => scenario.ProcessAppendedEvents(appended.Events),
            ReadModelFoldInput.Seeded seeded => scenario.ProcessEventsFor(seeded.EventSourceId, seeded.Events),
            _ => throw new ArgumentOutOfRangeException(nameof(input))
        });
        return scenario.Instance;
    }
}
