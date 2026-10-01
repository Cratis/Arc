// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring the embedded event model explorer.
/// </summary>
public static class EventModelViewerServiceCollectionExtensions
{
    /// <summary>
    /// Configures the event model explorer that <c>UseCratisEventModelViewer</c> exposes.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
    /// <param name="configure">An optional callback for configuring <see cref="EventModelViewerOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> for continuation.</returns>
    /// <remarks>
    /// Calling this is not what exposes the explorer - the application's build is, unless
    /// <see cref="EventModelViewerOptions.Enabled"/> says otherwise. This is where an application turns the
    /// explorer off, turns it on for a Release build it owns, puts it behind authorization, or names an assembly
    /// the automatic mapping would not find.
    /// </remarks>
    public static IServiceCollection AddCratisEventModelViewer(this IServiceCollection services, Action<EventModelViewerOptions>? configure = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions();
        if (configure is not null)
        {
            services.Configure(configure);
        }

        return services;
    }
}
