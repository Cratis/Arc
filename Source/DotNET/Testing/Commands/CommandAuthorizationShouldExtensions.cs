// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands;

/// <summary>Dependency-free assertions for standalone authorization results.</summary>
public static class CommandAuthorizationShouldExtensions
{
    /// <summary>Asserts that runtime authorization allowed the command.</summary>
    /// <param name="result">The evaluation result.</param>
    public static void ShouldBeAuthorized(this CommandAuthorizationResult result) => Expect(result, CommandAuthorizationOutcome.Authorized);

    /// <summary>Asserts that runtime authorization denied the command.</summary>
    /// <param name="result">The evaluation result.</param>
    public static void ShouldBeDenied(this CommandAuthorizationResult result) => Expect(result, CommandAuthorizationOutcome.Denied);

    /// <summary>Asserts that standalone evaluation is unsupported.</summary>
    /// <param name="result">The evaluation result.</param>
    public static void ShouldBeUnsupported(this CommandAuthorizationResult result) => Expect(result, CommandAuthorizationOutcome.Unsupported);

    static void Expect(CommandAuthorizationResult result, CommandAuthorizationOutcome expected)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.Outcome == expected) return;
        var reason = result.ReasonCode is { } code ? $" [{code}]" : string.Empty;
        throw new CommandAuthorizationAssertionFailed($"Expected command '{result.CommandType.FullName}' to be {expected}, but was {result.Outcome}{reason}: {result.Message}");
    }
}
