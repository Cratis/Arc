// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Screenplay.Embedded.Hosting;

/// <summary>
/// Represents the options for the automatically mapped event model explorer.
/// </summary>
/// <remarks>
/// <para>
/// These options are read by <c>UseCratisEventModelViewer</c> - the automatic mapping the Cratis meta-package
/// performs as part of <c>UseCratis</c>. Mapping the explorer yourself with <c>MapCratisEventModel</c> does not
/// consult them; that call maps what it is told to map, exactly as before.
/// </para>
/// <para>
/// Nothing here grants access. The explorer is served under the host's own pipeline, with the host's
/// authentication and authorization; <see cref="RequireAuthorization"/> only adds a requirement on top.
/// </para>
/// </remarks>
public class EventModelViewerOptions
{
    /// <summary>
    /// Gets or sets whether the explorer is exposed, or <see langword="null"/> to decide it from the build.
    /// </summary>
    /// <remarks>
    /// Left unset, the explorer is exposed only when the entry assembly was built without optimizations - a
    /// Debug build, as said by its <see cref="System.Diagnostics.DebuggableAttribute"/>. A Release-built
    /// application therefore exposes nothing unless it says so by setting this to <see langword="true"/>, and
    /// setting it to <see langword="false"/> turns the explorer off in every build.
    /// </remarks>
    public bool? Enabled { get; set; }

    /// <summary>
    /// Gets or sets whether the automatically mapped explorer requires an authorized caller. Defaults to false.
    /// </summary>
    /// <remarks>
    /// This adds the host's authorization to the explorer, its API and its assets alike. It needs the host to
    /// have added authentication and authorization services; without them, building the endpoints fails at
    /// startup rather than serving anything unprotected.
    /// </remarks>
    public bool RequireAuthorization { get; set; }

    /// <summary>
    /// Gets or sets the name of the authorization policy the explorer requires. Requires <see cref="RequireAuthorization"/>.
    /// </summary>
    /// <remarks>
    /// Unset, the host's default policy applies. The policy is the host's own - Arc does not register, relax or
    /// reclassify it.
    /// </remarks>
    public string? AuthorizationPolicy { get; set; }

    /// <summary>
    /// Gets the assemblies to serve embedded documents from, in addition to the ones found automatically.
    /// </summary>
    /// <remarks>
    /// The automatic mapping serves the entry assembly and the assemblies it references that are loaded and are
    /// not part of the framework or Cratis itself. An assembly that is not loaded when the application starts -
    /// or one named like a Cratis assembly - is added here.
    /// </remarks>
    public IList<Assembly> Assemblies { get; } = [];
}
