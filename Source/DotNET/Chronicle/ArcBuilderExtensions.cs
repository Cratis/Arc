// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc;

/// <summary>
/// Extension methods for <see cref="IArcBuilder"/> for adding Chronicle support.
/// </summary>
public static class ArcBuilderExtensions
{
    /// <summary>
    /// Adds Chronicle support to Arc.
    /// </summary>
    /// <param name="builder">The <see cref="IArcBuilder"/> to add to.</param>
    /// <param name="clientArtifactsProvider">Optional <see cref="IClientArtifactsProvider"/> to use. When not provided, it will
    /// attempt to resolve from the service collection (if Chronicle has already been registered) or fall back to
    /// <see cref="DefaultClientArtifactsProvider.Default"/>.</param>
    /// <returns><see cref="IArcBuilder"/> for continuation.</returns>
    public static IArcBuilder WithChronicle(this IArcBuilder builder, IClientArtifactsProvider? clientArtifactsProvider = null)
    {
        clientArtifactsProvider ??= builder.Services
            .LastOrDefault(d => d.ServiceType == typeof(IClientArtifactsProvider))
            ?.ImplementationInstance as IClientArtifactsProvider
            ?? DefaultClientArtifactsProvider.Default;

        clientArtifactsProvider.Initialize();

        builder.Services.AddAggregateRoots(builder.Types);
        builder.Services.AddReadModels(clientArtifactsProvider);

        return builder;
    }
}
