// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Cratis.Arc.Screenplay.Embedded.Hosting;
using Cratis.Arc.Screenplay.Embedded.Hosting.Assets;
using Cratis.Arc.Screenplay.Embedded.Hosting.Catalog;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for mapping the embedded event model explorer.
/// </summary>
public static class EventModelEndpoints
{
    /// <summary>
    /// The path the explorer and its API are served from.
    /// </summary>
    public const string Prefix = "/.cratis/event-model";

    static readonly ConditionalWeakTable<IEndpointRouteBuilder, Mapping> _mappings = new();

    /// <summary>
    /// Maps the embedded event model explorer, serving the Screenplay documents embedded in the given assemblies.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to map the explorer into.</param>
    /// <param name="assemblies">The assemblies to serve embedded documents from; the entry assembly when none are given.</param>
    /// <returns>The <see cref="RouteGroupBuilder"/> the explorer was mapped into, for the host to configure further.</returns>
    /// <exception cref="NoAssembliesToServeEventModelsFrom">Thrown when no assemblies are given and the process has no entry assembly.</exception>
    /// <exception cref="MalformedEventModelCatalog">Thrown when an assembly embeds a catalog that cannot be read as written.</exception>
    /// <remarks>
    /// Nothing is mapped until this is called, or until the Cratis meta-package's automatic mapping calls it for
    /// a Debug-built application running in Development (or one that explicitly enables automatic mapping).
    /// Explicit mapping works in any environment. The returned group is
    /// the seam for host policy; <c>MapCratisEventModel().RequireAuthorization()</c> puts the whole explorer, its
    /// API and its assets behind the host's authorization. Asked for twice on the same application, the explorer
    /// is mapped once: the second call adds its assemblies to what is served and gets the same group back, so
    /// automatic and explicit mapping together never make a request ambiguous.
    /// </remarks>
    public static RouteGroupBuilder MapCratisEventModel(this IEndpointRouteBuilder endpoints, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var sources = assemblies is { Length: > 0 }
            ? assemblies
            : [Assembly.GetEntryAssembly() ?? throw new NoAssembliesToServeEventModelsFrom()];

        return Map(endpoints, sources);
    }

    /// <summary>
    /// Maps the explorer for the given sources, or extends the one already mapped into the route builder.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to map the explorer into.</param>
    /// <param name="sources">The assemblies to serve embedded documents from.</param>
    /// <returns>The <see cref="RouteGroupBuilder"/> the explorer is mapped into.</returns>
    internal static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints, IReadOnlyList<Assembly> sources)
    {
        if (!_mappings.TryGetValue(endpoints, out var mapping))
        {
            var served = new EventModelViewerSources();
            mapping = new Mapping(served, MapRoutes(endpoints, served));
            _mappings.Add(endpoints, mapping);
        }

        mapping.Sources.Include(sources);
        return mapping.Group;
    }

    static RouteGroupBuilder MapRoutes(IEndpointRouteBuilder endpoints, EventModelViewerSources sources)
    {
        var assets = EmbeddedViewerAssets.Viewer;

        var group = endpoints.MapGroup(Prefix);
        group.AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers.CacheControl = "no-store";
            return await next(context);
        });

        // The handlers read the explorer through the sources rather than closing over it, so assemblies added by
        // a later call are served by the routes mapped here instead of by routes mapped a second time. Everything
        // the routes answer comes from EventModelExplorer and EmbeddedViewerAssets - these are only the HTTP
        // translation of them, which is what lets another host serve the same viewer over its own documents.

        // The explorer's assets are referenced relative to the document, so it has to be served from a path
        // that ends in a slash; asked for without one, it redirects to the one that does. The location keeps
        // the host's path base, since a browser resolves it against the origin rather than the application.
        group.MapGet("/", (HttpContext context) =>
            context.Request.Path.Value?.EndsWith('/') == true
                ? Index(assets)
                : Results.Redirect($"{context.Request.PathBase}{context.Request.Path}/"));
        group.MapGet("/hierarchy", () => Results.Json(sources.Explorer.Hierarchy, EventModelJson.SerializerOptions));
        group.MapGet("/documents/{projectId}/{documentId}/source", (string projectId, string documentId) => Source(sources.Explorer, projectId, documentId));
        group.MapGet("/documents/{projectId}/{documentId}/model", (string projectId, string documentId) => Model(sources.Explorer, projectId, documentId));
        group.MapGet("/assets/{**path}", (string? path) => Asset(assets, path));

        return group;
    }

    static IResult Index(EmbeddedViewerAssets assets) =>
        assets.TryReadIndex(out var content, out var contentType)
            ? Results.File(content, contentType)
            : Results.NotFound();

    static IResult Source(EventModelExplorer explorer, string projectId, string documentId) =>
        explorer.TryGetSource(projectId, documentId, out var source)
            ? Results.Text(source, "text/plain")
            : Results.NotFound();

    static IResult Model(EventModelExplorer explorer, string projectId, string documentId) =>
        explorer.TryGetModel(projectId, documentId, out var model)
            ? Results.Json(model, EventModelJson.SerializerOptions)
            : Results.NotFound();

    static IResult Asset(EmbeddedViewerAssets assets, string? path) =>
        assets.TryRead($"assets/{path}", out var content, out var contentType)
            ? Results.File(content, contentType)
            : Results.NotFound();

    /// <summary>
    /// The one explorer mapped into a route builder.
    /// </summary>
    /// <param name="Sources">What the explorer serves.</param>
    /// <param name="Group">The group it is mapped into.</param>
    sealed record Mapping(EventModelViewerSources Sources, RouteGroupBuilder Group);
}
