// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Connections;

/// <summary>
/// Represents an implementation of <see cref="IConnectionResolvers"/>.
/// </summary>
/// <param name="factory">The <see cref="IConnectionResolverFactory"/> that creates resolvers from options.</param>
/// <param name="arcOptions">The <see cref="IOptions{TOptions}"/> for <see cref="ArcOptions"/>.</param>
[Singleton]
public class ConnectionResolvers(IConnectionResolverFactory factory, IOptions<ArcOptions> arcOptions) : IConnectionResolvers
{
    readonly Lazy<ICachingConnectionResolver?> _application = new(() =>
        arcOptions.Value.ConnectionResolution.IsConfigured ? factory.Create(arcOptions.Value.ConnectionResolution) : null);

    readonly ConditionalWeakTable<ConnectionResolutionOptions, ICachingConnectionResolver> _integrations = [];

    /// <inheritdoc/>
    public ICachingConnectionResolver? For(ConnectionResolutionOptions options) =>
        options.IsConfigured
            ? _integrations.GetValue(options, factory.Create)
            : _application.Value;
}
