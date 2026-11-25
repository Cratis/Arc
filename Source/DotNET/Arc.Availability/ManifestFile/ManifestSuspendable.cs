// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.ManifestFile;

/// <summary>
/// A module, feature, slice or part an operator can suspend.
/// </summary>
/// <param name="Kind">One of <c>module</c>, <c>feature</c>, <c>slice</c> or <c>part</c>.</param>
/// <param name="Path">The path that is suspended: the slice location joined with <c>/</c> and, for a part, followed by its full name.</param>
/// <param name="Name">The name to show.</param>
public record ManifestSuspendable(string Kind, string Path, string Name);
