// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Build;

/// <summary>
/// Represents one file the build embeds into the assembly being compiled.
/// </summary>
/// <param name="Path">The full path of the file on disk.</param>
/// <param name="LogicalName">The name the file is embedded under.</param>
public record EmbeddedResourceFile(string Path, string LogicalName);
