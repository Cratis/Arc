// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Reflection;
using Microsoft.AspNetCore.StaticFiles;

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Assets;

/// <summary>
/// Serves the explorer's built assets out of the manifest resources of this assembly.
/// </summary>
/// <remarks>
/// Only resources embedded under the viewer prefix are reachable, and only by their exact logical name.
/// Nothing is resolved against a file system, so no path a request states can reach anything that was not
/// packaged as part of the viewer.
/// </remarks>
public sealed class EmbeddedViewerAssets
{
    /// <summary>
    /// The prefix the viewer's assets are embedded under.
    /// </summary>
    public const string ResourcePrefix = "Cratis.Arc.Screenplay.Embedded.Viewer.";

    /// <summary>
    /// The path of the document the explorer is served from.
    /// </summary>
    public const string IndexPath = "index.html";

    static readonly FileExtensionContentTypeProvider _contentTypes = new();

    readonly FrozenDictionary<string, Lazy<byte[]?>> _assets;

    EmbeddedViewerAssets(Assembly assembly, IEnumerable<string> paths)
    {
        _assets = paths.ToFrozenDictionary(_ => _, _ => new Lazy<byte[]?>(() => Read(assembly, _)), StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets whether the viewer was built into the package at all.
    /// </summary>
    public bool HasIndex => _assets.ContainsKey(IndexPath);

    /// <summary>
    /// Gets the assets embedded in an assembly.
    /// </summary>
    /// <param name="assembly">The assembly holding the viewer.</param>
    /// <returns>The resulting <see cref="EmbeddedViewerAssets"/>.</returns>
    public static EmbeddedViewerAssets For(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        return new(
            assembly,
            assembly.GetManifestResourceNames()
                .Where(name => name.StartsWith(ResourcePrefix, StringComparison.Ordinal))
                .Select(name => name[ResourcePrefix.Length..]));
    }

    /// <summary>
    /// Tries to read an asset.
    /// </summary>
    /// <param name="path">The path of the asset, relative to the root of the viewer.</param>
    /// <param name="content">When this method returns, holds the content of the asset.</param>
    /// <param name="contentType">When this method returns, holds the content type to serve the asset as.</param>
    /// <returns>True when the asset is embedded, false otherwise.</returns>
    public bool TryRead(string? path, out byte[] content, out string contentType)
    {
        content = [];
        contentType = "application/octet-stream";

        if (!IsReachable(path) || !_assets.TryGetValue(path!, out var asset))
        {
            return false;
        }

        if (asset.Value is not { } bytes)
        {
            return false;
        }

        content = bytes;
        if (_contentTypes.TryGetContentType(path!, out var resolved))
        {
            contentType = resolved;
        }

        return true;
    }

    static byte[]? Read(Assembly assembly, string path)
    {
        using var stream = assembly.GetManifestResourceStream($"{ResourcePrefix}{path}");
        if (stream is null)
        {
            return null;
        }

        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    /// <summary>
    /// Gets whether a path could name an embedded asset at all.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True when the path could name an asset, false otherwise.</returns>
    /// <remarks>
    /// The set of embedded names is the only thing that makes an asset reachable, so traversal cannot escape
    /// anywhere. It is rejected outright anyway, so a request that tries reads as the 404 it is rather than
    /// as a name that happens not to be embedded.
    /// </remarks>
    static bool IsReachable(string? path) =>
        !string.IsNullOrWhiteSpace(path) &&
        !path.StartsWith('/') &&
        !path.Contains('\\', StringComparison.Ordinal) &&
        !path.Split('/').Any(segment =>
            string.IsNullOrEmpty(segment) ||
            string.Equals(segment, ".", StringComparison.Ordinal) ||
            string.Equals(segment, "..", StringComparison.Ordinal));
}
