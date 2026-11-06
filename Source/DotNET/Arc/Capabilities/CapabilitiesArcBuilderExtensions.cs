// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Capabilities;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc;

/// <summary>
/// Provides extensions for hosting capabilities on a dedicated endpoint group.
/// </summary>
public static class CapabilitiesArcBuilderExtensions
{
    /// <summary>
    /// Requires a verified caller on the capability group's ports.
    /// </summary>
    /// <param name="builder">The Arc builder.</param>
    /// <param name="configure">An optional capability configuration delegate.</param>
    /// <returns>The builder for continuation.</returns>
    public static IArcBuilder WithCapabilities(this IArcBuilder builder, Action<CapabilitiesOptions>? configure = null)
    {
        builder.Services.Configure(configure ?? (_ => { }));
        builder.Services.TryAddTransient<CapabilityGateMiddleware>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, CapabilitiesStartupValidation>());
        return builder;
    }
}
