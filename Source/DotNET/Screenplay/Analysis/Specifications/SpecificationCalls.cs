// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Recognizes the calls a Chronicle integration specification is written with.
/// </summary>
/// <remarks>
/// Two ways of writing one are recognized, because Arc documents both. A specification driving the command pipeline
/// in process states what had happened through the scenario it holds and issues the command through the same
/// scenario; one driving a running host states it against the event log and issues the command over HTTP. Everything
/// is matched on the fully qualified metadata name of the type declaring the method, so neither testing package has
/// to be referenced for either to be read.
/// </remarks>
public static class SpecificationCalls
{
    /// <summary>The method appending one event to a sequence.</summary>
    public const string AppendMethod = "Append";

    /// <summary>The method appending several events to a sequence as one transaction.</summary>
    public const string AppendManyMethod = "AppendMany";

    /// <summary>The method stating the events that had happened for one event source.</summary>
    public const string EventsMethod = "Events";

    /// <summary>The method pinning the read model one event source resolves to.</summary>
    public const string ReadModelMethod = "ReadModel";

    /// <summary>The method issuing a command through the in-process pipeline.</summary>
    public const string ExecuteMethod = "Execute";

    /// <summary>The method taking a command as far as the pipeline validates it.</summary>
    public const string ValidateMethod = "Validate";

    /// <summary>The method issuing a command over HTTP.</summary>
    public const string ExecuteCommandMethod = "ExecuteCommand";

    /// <summary>The parameter carrying the single event an append is given.</summary>
    public const string EventParameter = "event";

    /// <summary>The parameter carrying the events an append or a scenario is given.</summary>
    public const string EventsParameter = "events";

    /// <summary>The parameter carrying the read model a scenario is pinned with.</summary>
    public const string ReadModelParameter = "readModel";

    /// <summary>The parameter carrying the command issued over HTTP.</summary>
    public const string CommandParameter = "command";

    /// <summary>
    /// Determines whether a call states the events a specification starts from.
    /// </summary>
    /// <param name="method">The method being called.</param>
    /// <returns>True when the call states events.</returns>
    public static bool IsGivenEvents(IMethodSymbol method) =>
        (IsOn(method, WellKnownTypeNames.EventSequence) && (Named(method, AppendMethod) || Named(method, AppendManyMethod))) ||
        (IsOn(method, WellKnownTypeNames.CommandScenarioSourceGivenBuilder) && Named(method, EventsMethod));

    /// <summary>
    /// Determines whether a call pins the read model a specification starts from.
    /// </summary>
    /// <param name="method">The method being called.</param>
    /// <returns>True when the call pins a read model.</returns>
    public static bool IsGivenReadModel(IMethodSymbol method) =>
        IsOn(method, WellKnownTypeNames.CommandScenarioSourceGivenBuilder) && Named(method, ReadModelMethod);

    /// <summary>
    /// Determines whether a call issues the command a specification is about.
    /// </summary>
    /// <param name="method">The method being called.</param>
    /// <returns>True when the call issues a command.</returns>
    public static bool IsExecution(IMethodSymbol method) =>
        (IsOn(method, WellKnownTypeNames.CommandScenario) && (Named(method, ExecuteMethod) || Named(method, ValidateMethod))) ||
        (IsOn(method, WellKnownTypeNames.HttpClientExtensions) && Named(method, ExecuteCommandMethod));

    /// <summary>
    /// Gets the name of the parameter carrying what a recognized call is given.
    /// </summary>
    /// <param name="method">The method being called.</param>
    /// <returns>The parameter name, or <see langword="null"/> when the call carries nothing this reads.</returns>
    public static string? PayloadParameterOf(IMethodSymbol method)
    {
        if (IsGivenReadModel(method))
        {
            return ReadModelParameter;
        }

        if (IsExecution(method))
        {
            return Named(method, ExecuteCommandMethod) ? CommandParameter : method.Parameters.FirstOrDefault()?.Name;
        }

        if (!IsGivenEvents(method))
        {
            return null;
        }

        return Named(method, AppendMethod) ? EventParameter : EventsParameter;
    }

    /// <summary>
    /// Determines whether a method carries a name.
    /// </summary>
    /// <param name="method">The method to check.</param>
    /// <param name="name">The name to match.</param>
    /// <returns>True when the names are the same.</returns>
    static bool Named(IMethodSymbol method, string name) => string.Equals(method.Name, name, StringComparison.Ordinal);

    /// <summary>
    /// Determines whether a method belongs to a named type, following the type it was reduced from.
    /// </summary>
    /// <param name="method">The method to check.</param>
    /// <param name="fullMetadataName">The fully qualified metadata name of the declaring type.</param>
    /// <returns>True when the method belongs to that type.</returns>
    /// <remarks>
    /// A specialization of an interface inherits its members without redeclaring them, so the interface a member is
    /// declared on is matched as well as the one it is reached through - appending to the event log is appending to a
    /// sequence, whichever of the two the call site names.
    /// </remarks>
    static bool IsOn(IMethodSymbol method, string fullMetadataName)
    {
        var declaring = (method.ReducedFrom ?? method).ContainingType;

        return declaring.Is(fullMetadataName) || declaring.FindInterface(fullMetadataName) is not null;
    }
}
