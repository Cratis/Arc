// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Identity.ForwardedIdentityV2;

namespace Cratis.Arc;

/// <summary>
/// Extensions for explicitly installing verified callers.
/// </summary>
public static class VerifiedCallersArcBuilderExtensions
{
    /// <summary>
    /// Installs signed v2 browser caller verification for one receiving resource.
    /// </summary>
    /// <param name="builder">The Arc builder.</param>
    /// <param name="configure">The explicit resource and trust configuration.</param>
    /// <returns>The Arc builder for continued configuration.</returns>
    /// <exception cref="InvalidVerifiedCallerInstallation">The installation is incomplete or duplicated.</exception>
    /// <exception cref="ForwardedIdentityNonceStoreNotConfigured">No replay store was selected.</exception>
    public static IArcBuilder WithVerifiedCallers(this IArcBuilder builder, Action<VerifiedCallersBuilder> configure)
    {
        var installation = new VerifiedCallersBuilder();
        configure(installation);
        installation.Install(builder.Services);
        return builder;
    }
}
