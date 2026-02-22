// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Provides extension methods for <see cref="WebApplication"/> for configuring culture settings.
/// </summary>
public static class CultureWebApplicationExtensions
{
    /// <summary>
    /// Use invariant culture request localization middleware in the application pipeline.
    /// </summary>
    /// <param name="app"><see cref="WebApplication"/> to extend.</param>
    /// <returns><see cref="WebApplication"/> for building continuation.</returns>
    /// <remarks>
    /// This adds the request localization middleware configured to use InvariantCulture.
    /// Should be called after UseInvariantCulture() on the WebApplicationBuilder.
    /// </remarks>
    public static WebApplication UseInvariantCulture(this WebApplication app)
    {
        var localizationOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
        app.UseRequestLocalization(localizationOptions);

        return app;
    }
}
