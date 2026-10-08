// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Emission.Specifications;

/// <summary>
/// Decides whether a scenario whose only outcome is the command response can be stated.
/// </summary>
/// <remarks>
/// A specification with no expected events asserts that the command produced none. A scenario asserting only the
/// response is therefore stated only for a command proven to record no facts: one with no productions and a handler
/// whose return type and returned expression can carry no event. Anything else is left out, as it was before
/// response expectations were recovered, and must not count as a kept scenario anywhere else either.
/// </remarks>
public static class ResponseOnlyScenarios
{
    /// <summary>
    /// Determines whether a scenario's only outcome is a recovered response.
    /// </summary>
    /// <param name="specification">The scenario.</param>
    /// <returns>True when the scenario expects no events, states, or rejections, but a response.</returns>
    public static bool IsResponseOnly(SpecificationModel specification) =>
        specification.Returns is not null && !specification.Then.Any() && !specification.Errors.Any();

    /// <summary>
    /// Determines whether a scenario survives emission as far as its response is concerned.
    /// </summary>
    /// <param name="specification">The scenario.</param>
    /// <param name="command">The admitted command the scenario issues, if any.</param>
    /// <returns>False only for a response-only scenario the command cannot state faithfully.</returns>
    public static bool CanBeStated(SpecificationModel specification, CommandModel? command) =>
        !IsResponseOnly(specification) ||
        (RecordsNoFacts(command) && command!.Authoring is { Generated.Count: 0 } authoring &&
         (authoring.Response is not null || authoring.ResponseFields.Count > 0) &&
         specification.Returns!.Fits(authoring));

    /// <summary>
    /// Determines whether a command is proven to record no facts.
    /// </summary>
    /// <param name="command">The admitted command.</param>
    /// <returns>True when it has no productions and its handler is proven free of fact-recording behavior.</returns>
    public static bool RecordsNoFacts(CommandModel? command) => command is { HasNoFactBehavior: true } && !command.Produces.Any();
}
