// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Provides extension methods for <see cref="IApplicationBuilder"/> for configuring Cratis.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Activates the Cratis middleware and endpoints on the application. Call this after <c>AddCratis</c> to map
    /// Arc's command and query endpoints and start the Chronicle client. This is the paired counterpart to
    /// <c>AddCratis</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This calls <c>UseCratisArc</c> and <c>UseCratisChronicle</c> for you. If you split the setup — calling
    /// <c>AddCratisArc</c> and <c>WithChronicle</c> yourself instead of <c>AddCratis</c> — call those two
    /// activation methods yourself rather than <c>UseCratis</c>.
    /// </para>
    /// <para>
    /// It also exposes the embedded event model explorer for an application that was built without optimizations
    /// — a Debug build — and is running in the Development hosting environment. Other builds or environments
    /// expose nothing unless the application explicitly opts in with
    /// <c>AddCratisEventModelViewer(options =&gt; options.Enabled = true)</c>, which is also where the explorer is
    /// turned off or put behind the host's authorization.
    /// </para>
    /// </remarks>
    /// <param name="app"><see cref="IApplicationBuilder"/> to extend.</param>
    /// <returns><see cref="IApplicationBuilder"/> for continuation.</returns>
    public static IApplicationBuilder UseCratis(this IApplicationBuilder app)
    {
        app.UseCratisArc();
        app.UseCratisChronicle();
        app.UseCratisEventModelViewer();
        return app;
    }
}
