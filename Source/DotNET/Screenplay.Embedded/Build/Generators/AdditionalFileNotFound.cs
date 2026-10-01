// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Build.Generators;

/// <summary>
/// Exception that gets thrown when an additional file the generators are given is not on disk.
/// </summary>
/// <param name="path">The path the build stated.</param>
/// <remarks>
/// A generator driven by additional files generates nothing, or something different, when one of them is missing,
/// and says nothing about it. The build stated the file, so the file has to be there.
/// </remarks>
public class AdditionalFileNotFound(string path)
    : Exception($"The additional file '{path}' the generators are given does not exist.");
