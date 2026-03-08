// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
    /// resolve from the configured <see cref="ChronicleOptions"/> in the service collection, using the
    /// <see cref="ChronicleOptions.ArtifactsProvider"/> that was configured via the options pipeline.</param>
    /// <returns><see cref="IArcBuilder"/> for continuation.</returns>
    public static IArcBuilder WithChronicle(this IArcBuilder builder, IClientArtifactsProvider? clientArtifactsProvider = null)
    {
        if (clientArtifactsProvider is null)
        {
            var chronicleOptions = new ChronicleOptions();
            foreach (var configureOptions in builder.Services
                .Where(d => d.ServiceType == typeof(IConfigureOptions<ChronicleOptions>) && d.ImplementationInstance is IConfigureOptions<ChronicleOptions>)
                .Select(d => (IConfigureOptions<ChronicleOptions>)d.ImplementationInstance!))
            {
                configureOptions.Configure(chronicleOptions);
            }

            clientArtifactsProvider = chronicleOptions.ArtifactsProvider;
        }

        clientArtifactsProvider.Initialize();

        builder.Services.AddAggregateRoots(builder.Types);
        builder.Services.AddReadModels(clientArtifactsProvider);

        return builder;
    }
}
