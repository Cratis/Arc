// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries;
using Cratis.Arc.Queries.ModelBound;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Queries;

/// <summary>
/// Discovers query methods on the scenario's read model without scanning unrelated read models in the test assembly.
/// </summary>
/// <typeparam name="TReadModel">The read model under test.</typeparam>
/// <param name="services">The scenario service provider.</param>
internal sealed class ScenarioQueryPerformerProviders<TReadModel>(IServiceProvider services) : IQueryPerformerProviders
{
    readonly Dictionary<FullyQualifiedQueryName, IQueryPerformer> _performers = typeof(TReadModel)
        .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
        .Where(method => typeof(TReadModel).IsReadModel() && method.IsValidQueryFor(typeof(TReadModel)))
        .Select(method => new ModelBoundQueryPerformer(
            typeof(TReadModel),
            typeof(TReadModel).FullName ?? typeof(TReadModel).Name,
            method,
            services.GetRequiredService<IServiceProviderIsService>(),
            services.GetRequiredService<IServiceScopeFactory>(),
            scope => scope.GetRequiredService<IAuthorizationEvaluator>()))
        .ToDictionary(performer => performer.FullyQualifiedName, performer => (IQueryPerformer)performer);

    /// <inheritdoc/>
    public IEnumerable<IQueryPerformer> Performers => _performers.Values;

    /// <inheritdoc/>
    public bool TryGetPerformersFor(FullyQualifiedQueryName query, [NotNullWhen(true)] out IQueryPerformer? performer) =>
        _performers.TryGetValue(query, out performer);
}
