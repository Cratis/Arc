// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Represents a single validation rule declared on a property of a command.
/// </summary>
/// <param name="ErrorMessage">The message stated for a broken rule.</param>
/// <param name="RuleType">The kind of rule it is, such as <c>notEmpty</c>.</param>
public record CommandRule(string ErrorMessage, string RuleType);
