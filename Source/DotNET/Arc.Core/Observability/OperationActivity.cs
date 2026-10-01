// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Arc.Tenancy;
using Cratis.Arc.Validation;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Observability;

/// <summary>
/// Describes command and query spans in domain terms and records their outcomes on them.
/// </summary>
/// <remarks>
/// Nothing here reads a value from a command or query payload. Spans carry types, names, identifiers, member names and
/// outcomes only, so a value marked as personal data can never reach a telemetry backend through them.
/// </remarks>
internal static class OperationActivity
{
    /// <summary>
    /// The largest number of validation failure events recorded on one span.
    /// </summary>
    internal const int MaxValidationEvents = 16;

    /// <summary>
    /// Starts a child span of a command, carrying the command type.
    /// </summary>
    /// <param name="source">The <see cref="ActivitySource"/> to start the span on.</param>
    /// <param name="name">The name of the span, one of the <see cref="WellKnownTelemetryNames"/>.</param>
    /// <param name="commandType">The type of the command the span is part of.</param>
    /// <returns>The started <see cref="Activity"/>, or <see langword="null"/> when nothing listens.</returns>
    /// <remarks>
    /// The span keeps its stable name; the command type is an attribute. Tracing backends use the name to group
    /// spans, so a name per command type would break dashboards keyed on it and multiply what they have to index.
    /// </remarks>
    internal static Activity? StartCommandChild(ActivitySource source, string name, Type commandType)
    {
        var activity = source.StartActivity(name, ActivityKind.Internal);
        AddCommandType(activity, commandType);
        return activity;
    }

    /// <summary>
    /// Adds the command type to a span.
    /// </summary>
    /// <param name="activity">The <see cref="Activity"/> to add to, if any.</param>
    /// <param name="commandType">The type of the command.</param>
    internal static void AddCommandType(Activity? activity, Type commandType)
    {
        if (activity is { IsAllDataRequested: true })
        {
            activity.SetTag(WellKnownTelemetryNames.CommandType, commandType.FullName ?? commandType.Name);
        }
    }

    /// <summary>
    /// Adds the correlation id of an operation to its span.
    /// </summary>
    /// <param name="activity">The <see cref="Activity"/> to add to, if any.</param>
    /// <param name="correlationId">The <see cref="CorrelationId"/> of the operation.</param>
    internal static void AddCorrelationId(Activity? activity, CorrelationId correlationId)
    {
        if (activity is { IsAllDataRequested: true })
        {
            activity.SetTag(WellKnownTelemetryNames.CorrelationId, correlationId.ToString());
        }
    }

    /// <summary>
    /// Adds the tenant the operation ran for, when one was resolved while it ran.
    /// </summary>
    /// <param name="activity">The <see cref="Activity"/> to add to, if any.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> the operation ran in.</param>
    /// <remarks>
    /// Only a tenant already resolved is read. Asking the accessor for its current tenant would resolve and cache one,
    /// which is a side effect telemetry must not have on the operation it observes. The accessor keeps its tenant in an
    /// async local, and one resolved inside an awaited call is gone again once that call returns, so the pipelines call
    /// this where their authorization scope, and with it the tenant they run for, is in place.
    /// </remarks>
    internal static void AddResolvedTenant(Activity? activity, IServiceProvider serviceProvider)
    {
        if (activity is not { IsAllDataRequested: true })
        {
            return;
        }

        var accessor = serviceProvider.GetService<TenantIdAccessor>() ?? serviceProvider.GetService<ITenantIdAccessor>() as TenantIdAccessor;
        var tenant = accessor?.ExplicitTenant ?? accessor?.Cached;
        if (tenant is not null && tenant != TenantId.NotSet)
        {
            activity.SetTag(WellKnownTelemetryNames.Tenant, tenant.Value);
        }
    }

    /// <summary>
    /// Records the outcome of an operation on its span, as an attribute, events and, for an error, the status.
    /// </summary>
    /// <param name="activity">The <see cref="Activity"/> to record on, if any.</param>
    /// <param name="outcomeAttribute">The name of the outcome attribute.</param>
    /// <param name="outcome">The outcome, one of the <see cref="WellKnownOperationOutcomes"/>.</param>
    /// <param name="validationResults">The validation results that blocked the operation.</param>
    internal static void RecordOutcome(Activity? activity, string outcomeAttribute, string outcome, IEnumerable<ValidationResult> validationResults)
    {
        if (activity is not { IsAllDataRequested: true })
        {
            return;
        }

        activity.SetTag(outcomeAttribute, outcome);
        if (outcome == WellKnownOperationOutcomes.Success)
        {
            return;
        }

        // Following OpenTelemetry, only an error fails the span. A rejection by validation, authorization or the event
        // store is an expected business outcome, and a cancellation is the caller giving up: they are told apart by the
        // outcome attribute and the events below.
        if (outcome == WellKnownOperationOutcomes.Error)
        {
            activity.SetStatus(ActivityStatusCode.Error, outcome);
        }

        if (outcome == WellKnownOperationOutcomes.Authorization)
        {
            activity.AddEvent(new ActivityEvent(WellKnownTelemetryNames.AuthorizationDeniedEvent));
        }

        foreach (var result in validationResults.Take(MaxValidationEvents))
        {
            activity.AddEvent(ValidationFailed(result));
        }
    }

    /// <summary>
    /// Records an exception on a span, by type only.
    /// </summary>
    /// <param name="activity">The <see cref="Activity"/> to record on, if any.</param>
    /// <param name="exception">The <see cref="Exception"/> to record.</param>
    /// <remarks>
    /// The message and stack trace are left out: an exception message is free text and often quotes the value that
    /// caused it. They stay in the logs, which the application controls.
    /// </remarks>
    internal static void RecordException(Activity? activity, Exception exception)
    {
        if (activity is not { IsAllDataRequested: true })
        {
            return;
        }

        activity.AddEvent(new ActivityEvent(
            WellKnownTelemetryNames.ExceptionEvent,
            tags: new ActivityTagsCollection { { WellKnownTelemetryNames.ExceptionType, exception.GetType().FullName ?? exception.GetType().Name } }));
    }

    /// <summary>
    /// Records an exception thrown inside a child span, and fails the span when the exception is an error.
    /// </summary>
    /// <param name="activity">The <see cref="Activity"/> to record on, if any.</param>
    /// <param name="exception">The <see cref="Exception"/> to record.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> of the operation the span is part of.</param>
    internal static void RecordFailure(Activity? activity, Exception exception, CancellationToken cancellationToken)
    {
        if (activity is not { IsAllDataRequested: true })
        {
            return;
        }

        RecordException(activity, exception);
        if (IsError(exception, cancellationToken))
        {
            activity.SetStatus(ActivityStatusCode.Error, WellKnownOperationOutcomes.Error);
        }
    }

    /// <summary>
    /// Determines whether an exception is an error, as opposed to an expected outcome the pipeline turns into a result.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to check.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> of the operation.</param>
    /// <returns>True if the exception is an error; otherwise false.</returns>
    /// <remarks>
    /// An <see cref="IValidationFailure"/> becomes a validation outcome, and anything thrown once the operation was
    /// cancelled becomes a cancelled outcome. Neither fails a span, the same as on the operation span.
    /// </remarks>
    internal static bool IsError(Exception exception, CancellationToken cancellationToken) =>
        exception is not IValidationFailure && !cancellationToken.IsCancellationRequested;

    static ActivityEvent ValidationFailed(ValidationResult result)
    {
        var tags = new ActivityTagsCollection
        {
            { WellKnownTelemetryNames.ValidationSeverity, result.Severity.ToString() },
            { WellKnownTelemetryNames.ValidationMembers, result.Members.ToArray() },
            { WellKnownTelemetryNames.ValidationReason, result.Reason?.Value ?? ValidationResultReason.Rule.Value }
        };

        if (!string.IsNullOrEmpty(result.ReasonDetail))
        {
            tags.Add(WellKnownTelemetryNames.ValidationReasonDetail, result.ReasonDetail);
        }

        return new ActivityEvent(WellKnownTelemetryNames.ValidationFailedEvent, tags: tags);
    }
}
