// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis.Screens;

/// <summary>
/// Represents a name a user interface file imports, under the name the file calls it by.
/// </summary>
/// <param name="Local">The name the importing file calls it by.</param>
/// <param name="Name">The name as the module exports it.</param>
/// <param name="Module">The module specifier, exactly as it was written.</param>
public record ScreenImportBinding(string Local, string Name, string Module)
{
    /// <summary>
    /// Gets whether the module is a file sitting alongside the importing one, which is where a generated proxy is.
    /// </summary>
    public bool IsRelative => Module.StartsWith("./", StringComparison.Ordinal) || Module.StartsWith("../", StringComparison.Ordinal);
}
