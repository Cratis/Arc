// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents a constraint whose rule lives in code rather than in the declaration.
/// </summary>
/// <param name="Name">The name of the constraint.</param>
/// <param name="SourceFilePath">The path of the file implementing the constraint, if it is known.</param>
public record CustomConstraintModel(string Name, string? SourceFilePath) : ConstraintModel(Name);
