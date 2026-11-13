// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.Abilities;

/// <summary>
/// The outcome names every ability has. An ability may declare further business outcomes of its own.
/// </summary>
public static class AbilityOutcomes
{
    /// <summary>The ability ran and returned.</summary>
    public const string Returned = "returned";

    /// <summary>The ability could not do its work. Inside a command operation this fails the command.</summary>
    public const string Failed = "failed";
}
