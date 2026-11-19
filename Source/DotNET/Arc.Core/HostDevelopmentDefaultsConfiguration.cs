// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Arc;

/// <summary>
/// Sets the development-dependent defaults of <see cref="ArcOptions"/> from the host's <see cref="IHostEnvironment"/>.
/// </summary>
/// <remarks>
/// Only the default is set, never an explicit value, so the order of this configuration relative to configuration binding
/// and code configuration does not matter. Without a host the process-environment fallback in <see cref="ArcOptions"/> stands.
/// </remarks>
/// <param name="services">The <see cref="IServiceProvider"/> used to look up the optional <see cref="IHostEnvironment"/>.</param>
[IgnoreConvention]
internal sealed class HostDevelopmentDefaultsConfiguration(IServiceProvider services) : IConfigureOptions<ArcOptions>
{
    /// <inheritdoc/>
    public void Configure(ArcOptions options)
    {
        var environment = services.GetService<IHostEnvironment>();
        if (environment is not null)
        {
            options.HostDevelopmentDefault = environment.IsDevelopment();
        }
    }
}
