// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Generation;

/// <summary>
/// Represents what part of an application an embedded Screenplay document describes.
/// </summary>
/// <remarks>
/// The kind is what a navigator groups by, so it is part of the catalog rather than something a reader infers from
/// how many segments an identifier happens to have.
/// </remarks>
public enum EmbeddedDocumentKind
{
    /// <summary>
    /// The whole of what one assembly declares, rooted at its root namespace.
    /// </summary>
    Assembly = 0,

    /// <summary>
    /// One namespace directly beneath the root namespace that has features beneath it.
    /// </summary>
    Module = 1,

    /// <summary>
    /// One namespace holding slices, either directly beneath the root namespace or beneath a module.
    /// </summary>
    Feature = 2
}
