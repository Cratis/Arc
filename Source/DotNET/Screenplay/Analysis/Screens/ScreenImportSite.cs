// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis.Screens;

/// <summary>
/// Represents an import a screen wrote, together with where it was written from.
/// </summary>
/// <param name="Namespace">The namespace of the slice the screen belongs to.</param>
/// <param name="Screen">The name of the screen.</param>
/// <param name="Directory">The directory the file realizing the screen sits in, which the module is relative to.</param>
/// <param name="Import">The name it imported and the module it came from.</param>
/// <remarks>
/// Where the import was written is kept because a module specifier means nothing on its own - it is a path from the
/// file that wrote it, and only that file says where it starts.
/// </remarks>
public record ScreenImportSite(string Namespace, string Screen, string Directory, ScreenImport Import);
