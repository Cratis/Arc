// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Registers typed read protection dispatch per read model, so resolution needs no per-request generic instantiation.
/// </summary>
/// <remarks>
/// <c>AddReadModels</c> registers every Chronicle-backed read model. This registry does not imply
/// that the rest of Arc supports NativeAOT. A model reached without <c>AddReadModels</c> is registered on first use.
/// Registrations are process-wide and keep the model <see cref="Type"/> alive, so a collectible
/// <c>AssemblyLoadContext</c> holding a read model cannot unload.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ReadModelDispatch
{
    static readonly ConcurrentDictionary<Type, ReadModelDispatchEntry> _entries = new();

    /// <summary>
    /// Registers typed dispatch for a read model. The first registration wins.
    /// </summary>
    /// <typeparam name="TModel">The read model type.</typeparam>
    public static void Register<TModel>()
        where TModel : class => _entries.TryAdd(
            typeof(TModel),
            new ReadModelDispatchEntry(
                ChronicleParameterDependencyResolver.ResolveModel<TModel>,
                PerStreamReadModelBinding.ResolveModel<TModel>,
                reads => reads.Describe<TModel>()));

    /// <summary>
    /// Gets the dispatch for a read model.
    /// </summary>
    /// <param name="model">The read model type.</param>
    /// <returns>The typed dispatch.</returns>
    /// <exception cref="MissingReadModelDispatch">The model is not a class or cannot be registered.</exception>
    internal static ReadModelDispatchEntry For(Type model)
    {
        if (_entries.TryGetValue(model, out var entry))
        {
            return entry;
        }

        // The registration may live in a module initializer that has not run yet.
        RuntimeHelpers.RunModuleConstructor(model.Module.ModuleHandle);
        if (!_entries.TryGetValue(model, out entry) && ReadModelServiceCollectionExtensions.TryRegisterDispatch(model))
        {
            _entries.TryGetValue(model, out entry);
        }

        return entry ?? throw new MissingReadModelDispatch(model);
    }
}
