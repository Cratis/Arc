// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Generation;

/// <summary>
/// Holds the logical names the generated resources are embedded under.
/// </summary>
/// <remarks>
/// The names are fixed rather than derived from the consuming assembly, because the host reading them back knows
/// only that it is looking at an assembly built with this package. An assembly that embedded its documents under
/// its own name would be one nothing could find them in without guessing.
/// </remarks>
public static class EmbeddedResourceNames
{
    /// <summary>
    /// The prefix every resource this package embeds is named with.
    /// </summary>
    public const string Prefix = "Cratis.Arc.Screenplay.Embedded.";

    /// <summary>
    /// The logical name of the JSON catalog describing every embedded document.
    /// </summary>
    public const string Catalog = Prefix + "catalog.json";

    /// <summary>
    /// The prefix every embedded <c>.play</c> document is named with.
    /// </summary>
    public const string Documents = Prefix + "documents.";

    /// <summary>
    /// The extension every embedded document is named with.
    /// </summary>
    public const string DocumentExtension = ".play";

    /// <summary>
    /// Gets the logical name the document describing a namespace is embedded under.
    /// </summary>
    /// <param name="id">The identifier of the document, which is the namespace it describes.</param>
    /// <returns>The logical name.</returns>
    public static string ForDocument(string id) => $"{Documents}{id}{DocumentExtension}";
}
