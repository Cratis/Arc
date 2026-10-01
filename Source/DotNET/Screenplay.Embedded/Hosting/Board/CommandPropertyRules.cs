// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Represents the validation rules declared for a single property of a command.
/// </summary>
/// <param name="PropertyName">The name of the property the rules are declared on.</param>
/// <param name="Rules">The rules declared on the property.</param>
public record CommandPropertyRules(string PropertyName, IReadOnlyList<CommandRule> Rules);
