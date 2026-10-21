// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// The exception that is thrown when a nested command tries to commit its enforcing parent's unit.
/// </summary>
/// <param name="nested">The nested command.</param>
/// <param name="parent">The owning command.</param>
public class NestedEarlyCompletionUnderReadProtection(Type nested, Type parent)
    : Exception($"{nested.Name} cannot commit {parent.Name}'s transaction while read protection is On. Commit in the outer command.");
