// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>Observes extraction only within an explicitly enabled asynchronous scope.</summary>
internal static class ValidationProjectionRecorder
{
    static AsyncLocal<State?>? _state;

    static State? Current => _state?.Value;

    internal static bool IsEnabled => Current is not null;

    internal static bool IsConcept => Current?.Context.Source == ValidationProjectionSource.Concept;

    internal static bool IsOptionalConcept => IsConcept && Current!.Context.Optional;

    internal static IDisposable? BeginReport(ValidationProjectionReport? report)
    {
        if (report is null) return null;
        if (_state is null) Interlocked.CompareExchange(ref _state, new AsyncLocal<State?>(), null);
        return Enter(new(report, new(null)));
    }

    internal static IDisposable? Begin(ValidationProjectionEndpoint endpoint)
    {
        var current = Current;
        return current is null ? null : Enter(current with { Context = new(endpoint) });
    }

    internal static IDisposable? Member(string member)
    {
        var current = Current;
        if (current is null || IsConcept) return null;
        return Enter(current with { Context = current.Context with { Member = member } });
    }

    internal static IDisposable? Concept(string member, bool optional)
    {
        var current = Current;
        return current is null ? null : Enter(current with { Context = current.Context with { Member = member, Source = ValidationProjectionSource.Concept, Optional = optional } });
    }

    internal static IDisposable? Annotation(string member)
    {
        var current = Current;
        return current is null ? null : Enter(current with { Context = current.Context with { Member = member, Source = ValidationProjectionSource.DataAnnotation } });
    }

    internal static void Record(ValidationRuleKind kind, string outcome, string reason, int? severity = null)
    {
        var current = Current;
        if (current?.Context.Endpoint is not { } endpoint) return;
        current.Report.Add(new(endpoint, current.Context.Member, kind, current.Context.Source, outcome, reason, severity));
    }

    internal static void OverrideAnnotation(PropertyValidationDescriptor annotation)
    {
        var current = Current;
        if (current?.Context.Endpoint is { } endpoint) current.Report.OverrideAnnotation(endpoint, annotation.PropertyName);
    }

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
        public void Dispose() => storage.Value = previous;
    }
}
