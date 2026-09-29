// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc;

/// <summary>
/// Represents a project assembly that could not be loaded while registering generated metadata.
/// </summary>
/// <param name="Name">The name of the assembly.</param>
/// <param name="Error">The error that prevented loading it.</param>
internal sealed record SkippedProjectAssembly(string Name, Exception Error);
