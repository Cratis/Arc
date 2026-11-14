// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>Observes extraction only within an explicitly enabled asynchronous scope.</summary>
internal static class ValidationProjectionRecorder
{
    static AsyncLocal<State?>? _state;

    /// <summary>Gets a value indicating whether reporting is enabled for the current execution.</summary>
    internal static bool IsEnabled => Current is not null;

    /// <summary>Gets a value indicating whether the current source is a concept validator.</summary>
    internal static bool IsConcept => Current?.Context.Source == ValidationProjectionSource.Concept;

    /// <summary>Gets a value indicating whether the current concept member is optional.</summary>
    internal static bool IsOptionalConcept => IsConcept && Current!.Context.Optional;

    static State? Current => _state?.Value;

    /// <summary>Enables observation for one generation, if requested.</summary>
    /// <param name="report">The report, or null to leave observation disabled.</param>
    /// <returns>A scope restoring the previous recorder, or null when disabled.</returns>
    internal static IDisposable? BeginReport(ValidationProjectionReport? report)
    {
        if (report is null) return null;
        if (_state is null) Interlocked.CompareExchange(ref _state, new AsyncLocal<State?>(), null);
        return Enter(new(report, new(null)));
    }

    /// <summary>Begins observing an endpoint.</summary>
    /// <param name="endpoint">The endpoint being extracted.</param>
    /// <returns>A restoring scope, or null when disabled.</returns>
    internal static IDisposable? Begin(ValidationProjectionEndpoint endpoint)
    {
        var current = Current;
        return current is null ? null : Enter(current with { Context = new(endpoint) });
    }

    /// <summary>Begins observing an ordinary validator member.</summary>
    /// <param name="member">The camelCase member name.</param>
    /// <returns>A restoring scope, or null when disabled or already observing a concept.</returns>
    internal static IDisposable? Member(string member)
    {
        var current = Current;
        if (current is null || IsConcept) return null;
        return Enter(current with { Context = current.Context with { Member = member } });
    }

    /// <summary>Begins observing concept rules for their owning member.</summary>
    /// <param name="member">The camelCase owning member name.</param>
    /// <param name="optional">Whether inferred presence rules are omitted.</param>
    /// <returns>A restoring scope, or null when disabled.</returns>
    internal static IDisposable? Concept(string member, bool optional)
    {
        var current = Current;
        return current is null ? null : Enter(current with { Context = current.Context with { Member = member, Source = ValidationProjectionSource.Concept, Optional = optional } });
    }

    /// <summary>Begins observing an annotated member.</summary>
    /// <param name="member">The camelCase member name.</param>
    /// <returns>A restoring scope, or null when disabled.</returns>
    internal static IDisposable? Annotation(string member)
    {
        var current = Current;
        return current is null ? null : Enter(current with { Context = current.Context with { Member = member, Source = ValidationProjectionSource.DataAnnotation } });
    }

    /// <summary>Records an allowlisted extraction decision.</summary>
    /// <param name="kind">The coarse rule family.</param>
    /// <param name="outcome">The projection outcome.</param>
    /// <param name="reason">The stable reason code.</param>
    /// <param name="severity">The known severity, if available.</param>
    internal static void Record(ValidationRuleKind kind, string outcome, string reason, int? severity = null)
    {
        var current = Current;
        if (current?.Context.Endpoint is not { } endpoint) return;
        current.Report.Add(new(endpoint, current.Context.Member, kind, current.Context.Source, outcome, reason, severity));
    }

    /// <summary>Marks annotation decisions overridden by higher-precedence rules.</summary>
    /// <param name="annotation">The overridden annotation's member descriptor.</param>
    internal static void OverrideAnnotation(PropertyValidationDescriptor annotation)
    {
        var current = Current;
        if (current?.Context.Endpoint is { } endpoint) current.Report.OverrideAnnotation(endpoint, annotation.PropertyName);
    }

    /// <summary>Records a type-level omission when an endpoint has no projectable rules.</summary>
    internal static void CompleteEndpoint()
    {
        var current = Current;
        if (current?.Context.Endpoint is { } endpoint) current.Report.CompleteEndpoint(endpoint);
    }

    static IDisposable Enter(State state)
    {
        var storage = _state!;
        var scope = new Scope(storage, storage.Value);
        storage.Value = state;
        return scope;
    }

    sealed record State(ValidationProjectionReport Report, ValidationProjectionContext Context);

    sealed class Scope(AsyncLocal<State?> storage, State? previous) : IDisposable
    {
        /// <inheritdoc/>
        public void Dispose() => storage.Value = previous;
    }
}
