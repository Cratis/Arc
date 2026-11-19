// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Emission;

/// <summary>
/// What the commands of one slice are resolved against.
/// </summary>
/// <param name="Namespace">The namespace of the slice, for use in diagnostics.</param>
/// <param name="Declared">The names of the events and commands the slice declares.</param>
/// <param name="DuplicateOperations">The operation names the slice declares more than once.</param>
internal sealed record AuthoringSlice(string Namespace, HashSet<string> Declared, HashSet<string> DuplicateOperations);
