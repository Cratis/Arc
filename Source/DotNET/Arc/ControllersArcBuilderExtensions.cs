// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;

namespace Cratis.Arc;

/// <summary>
/// Provides extension methods on <see cref="IArcBuilder"/> for MVC controllers.
/// </summary>
public static class ControllersArcBuilderExtensions
{
    static readonly object _controllersOffKey = new();

    /// <summary>
    /// Leaves MVC out of the application: Arc does not register MVC or discover controllers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Model-bound commands and queries, including observable queries, keep working, because Arc maps their
    /// endpoints itself. Controller-based commands and queries are not available, and neither is anything else that
    /// needs MVC, such as <c>MapControllers</c>.
    /// </para>
    /// <para>
    /// MVC is not supported with trimming or NativeAOT. This keeps MVC out of the application at runtime, but it is a
    /// runtime choice: the trimmer cannot remove MVC from a trimmed or NativeAOT publish because of it. To have MVC
    /// removed, set the <c>CratisArcControllersSupport</c> MSBuild property to <see langword="false"/> in the application's
    /// project file, which turns controllers off without calling this. By default controllers are on.
    /// </para>
    /// <para>
    /// Without MVC the application registers the ASP.NET Core services it used to get from MVC itself, such as
    /// <c>AddCors</c> for <c>UseCors</c> and <c>AddEndpointsApiExplorer</c> for Swashbuckle. Arc registers
    /// authorization.
    /// </para>
    /// </remarks>
    /// <param name="builder"><see cref="IArcBuilder"/> to configure.</param>
    /// <returns><see cref="IArcBuilder"/> for building continuation.</returns>
    /// <example>
    /// <code>
    /// builder.AddCratisArc(configureBuilder: arc => arc.WithoutControllers());
    /// </code>
    /// </example>
    public static IArcBuilder WithoutControllers(this IArcBuilder builder)
    {
        builder.AppBuilder.Properties[_controllersOffKey] = true;
        return builder;
    }

    /// <summary>
    /// Gets whether controllers are turned on for the application being built.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> for the application.</param>
    /// <returns>
    /// True if controllers are on, false if <see cref="WithoutControllers"/> or the controllers feature switch turned
    /// them off.
    /// </returns>
    internal static bool ControllersAreOn(this IHostApplicationBuilder builder) =>
        ArcFeatureSwitches.ControllersSupportIsOn && !builder.Properties.ContainsKey(_controllersOffKey);
}
