// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands;

/// <summary>The command wiring check could not verify every declaration.</summary>
/// <param name="problems">The ordered wiring failures.</param>
public sealed class CommandsAreNotExecutable(IReadOnlyList<CommandWiringProblem> problems)
    : Exception($"Command wiring could not be verified ({problems.Count} problems):{Environment.NewLine}{string.Join(Environment.NewLine, problems)}")
{
    /// <summary>Gets the wiring failures.</summary>
    public IReadOnlyList<CommandWiringProblem> Problems { get; } = problems;
}
