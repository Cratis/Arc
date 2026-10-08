// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Writes out what a specification states that the board has no card for, the way Screenplay writes it.
/// </summary>
/// <remarks>
/// The board draws given events, the action and the expected events and errors. A read model given or
/// expected, the caller, the clock, generated values, the expected response and the read models expected
/// to be absent have no card; the board shows the specification's name in its header, so they travel there
/// rather than being dropped.
/// </remarks>
internal static class SpecificationDetails
{
    /// <summary>
    /// The note carried with an explicit event route - a route is syntax only in the Screenplay language.
    /// </summary>
    public const string RouteAvailability = "Syntax-only (PLAY0268); the board displays routing intent, not an executable route assertion.";

    /// <summary>
    /// Gets the name the board shows for a specification.
    /// </summary>
    /// <param name="specification">The specification.</param>
    /// <returns>The name, followed by every detail without a card.</returns>
    public static string NameOf(SpecificationSyntax specification)
    {
        var details = Of(specification).ToList();
        return details.Count == 0 ? specification.Name : $"{specification.Name} — {string.Join(" | ", details)}";
    }

    /// <summary>
    /// Gets the name the board shows for an event step, with its route when the specification states one.
    /// </summary>
    /// <param name="event">The event step.</param>
    /// <returns>The name.</returns>
    /// <remarks>
    /// An event source stated with <see langword="for"/> alone is part of the name too, because it says which stream the event
    /// is on and the card has no other place for it.
    /// </remarks>
    public static string StepName(SpecificationEventSyntax @event) => @event switch
    {
        _ when IsRouted(@event) => $"{@event.EventType} — {RouteOf(@event)}; {RouteAvailability}",
        { For: { } source } => $"{@event.EventType} — for {SpecificationValues.TextOf(source)}",
        _ => @event.EventType
    };

    static bool IsRouted(SpecificationEventSyntax @event) => @event.Stream is not null || @event.NoStream is not null;

    static string RouteOf(SpecificationEventSyntax @event) => string.Join("; ", new[]
    {
        @event.For is null ? null : $"for {SpecificationValues.TextOf(@event.For)}",
        @event.Stream is null ? "no stream" : $"stream {@event.Stream.EventSource}.{@event.Stream.Stream}",
        @event.Stream?.StreamId is null ? null : $"streamId = {SpecificationValues.TextOf(@event.Stream.StreamId.Source)}"
    }.OfType<string>());

    static IEnumerable<string> Of(SpecificationSyntax specification)
    {
        var routes = Routes(specification).ToList();
        foreach (var route in routes)
        {
            yield return route;
        }

        if (routes.Count > 0)
        {
            yield return RouteAvailability;
        }

        foreach (var detail in Given(specification).Concat(Then(specification)))
        {
            yield return detail;
        }
    }

    static IEnumerable<string> Routes(SpecificationSyntax specification)
    {
        var steps = (specification.Given ?? []).Select((@event, index) => (@event, role: $"given {index + 1}"))
            .Concat(specification.WhenAppended is null ? [] : [(specification.WhenAppended, "when append")])
            .Concat((specification.ThenEvents ?? []).Select((@event, index) => (@event, role: $"then {index + 1}")));

        return steps
            .Where(step => IsRouted(step.@event))
            .Select(step => $"{step.role}: {step.@event.EventType} — {RouteOf(step.@event)}");
    }

    static IEnumerable<string> Given(SpecificationSyntax specification)
    {
        if (specification.File is { } file)
        {
            yield return $"file {file.Path}";
        }

        if (specification.GivenCaller is { } caller)
        {
            yield return Caller(caller);
        }

        if (specification.GivenClock is { } clock)
        {
            yield return $"given clock \"{clock.Instant}\"";
        }

        foreach (var capture in specification.GivenCaptures ?? [])
        {
            yield return $"given capture {capture.Capture} {{ {SpecificationValues.TextOf(capture.Record)} }}";
        }

        foreach (var failure in specification.GivenOperationFailures ?? [])
        {
            yield return $"given operation {failure.Operation} fails";
        }

        foreach (var model in specification.GivenReadModels ?? [])
        {
            yield return $"given readmodel {model.Name} {{ {SpecificationValues.TextOf(model.Properties)} }}";
        }

        if (specification.When?.For is { } source)
        {
            yield return $"when for {SpecificationValues.TextOf(source)}";
        }

        if (specification.When?.GeneratedValues is { } generated && generated.Any())
        {
            yield return $"generated (not request inputs): {SpecificationValues.TextOf(generated)}";
        }
    }

    static IEnumerable<string> Then(SpecificationSyntax specification)
    {
        if (specification.ThenEventsInAnyOrder)
        {
            yield return "then events in any order";
        }

        foreach (var model in specification.ThenReadModels ?? [])
        {
            yield return $"then readmodel {model.Name}{Exactly(model.Exactly)} {{ {SpecificationValues.TextOf(model.Properties)} }}";
        }

        foreach (var absent in specification.ThenAbsentReadModels ?? [])
        {
            yield return $"then no readmodel {absent.Name} for {SpecificationValues.TextOf(absent.Key)}";
        }

        foreach (var query in specification.ThenQueries ?? [])
        {
            var results = string.Join(' ', query.Results.Select(result => $"result {{ {SpecificationValues.TextOf(result.Properties)} }}"));
            yield return $"then query {query.Query}{Exactly(query.Exactly)} arguments {{ {SpecificationValues.TextOf(query.Arguments)} }} {results}".TrimEnd();
        }

        foreach (var result in specification.ThenResults ?? [])
        {
            yield return $"then result{Exactly(result.Exactly)} {{ {SpecificationValues.TextOf(result.Properties)} }}";
        }

        if (specification.ThenNoResult is not null)
        {
            yield return "then no result";
        }

        if (specification.ThenReturns is { } returns)
        {
            yield return Returns(returns);
        }

        foreach (var operation in specification.ThenOperations ?? [])
        {
            yield return $"then operation {operation.Operation} {{ {SpecificationValues.TextOf(operation.Values)} }}";
        }

        foreach (var compensated in specification.ThenCompensated ?? [])
        {
            yield return $"then compensated {compensated.Operation}";
        }
    }

    static string Exactly(bool exactly) => exactly ? " exactly" : string.Empty;

    static string Returns(SpecificationReturnSyntax returns) => returns switch
    {
        ScalarSpecificationReturnSyntax scalar => $"then returns {SpecificationValues.TextOf(scalar.Value)}",
        RecordSpecificationReturnSyntax record => $"then returns {{ {SpecificationValues.TextOf(record.Fields)} }}",
        _ => "then returns"
    };

    static string Caller(SpecificationCallerSyntax caller)
    {
        var parts = new List<string>();
        if (caller.Authenticated)
        {
            parts.Add("authenticated");
        }

        parts.AddRange((caller.Roles ?? []).Select(role => $"role \"{role}\""));
        parts.AddRange((caller.Claims ?? []).Select(claim => $"claim \"{claim.Type}\" = \"{claim.Value}\""));
        return parts.Count == 0 ? "given caller" : $"given caller {string.Join(", ", parts)}";
    }
}
