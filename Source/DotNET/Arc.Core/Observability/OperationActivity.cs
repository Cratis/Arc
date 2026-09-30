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
    /// Names a span after the operation it covers and adds the correlation id.
    /// </summary>
    /// <param name="activity">The <see cref="Activity"/> to describe, if any.</param>
    /// <param name="displayName">The name to show for the span, such as the command type name.</param>
    /// <param name="correlationId">The <see cref="CorrelationId"/> of the operation.</param>
    internal static void Describe(Activity? activity, string displayName, CorrelationId correlationId)
    {
        if (activity is not { IsAllDataRequested: true })
        {
            return;
        }

        activity.DisplayName = displayName;
        activity.SetTag(WellKnownTelemetryNames.CorrelationId, correlationId.ToString());
    }

    /// <summary>
    /// Adds the tenant the operation ran for, when one was resolved while it ran.
    /// </summary>
    /// <param name="activity">The <see cref="Activity"/> to add to, if any.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> the operation ran in.</param>
    /// <remarks>
    /// Only a tenant already resolved is read. Asking the accessor for its current tenant would resolve and cache one,
    /// which is a side effect telemetry must not have on the operation it observes.
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
        // store is an expected business outcome: it is told apart by the outcome attribute and the events below.
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
    /// Gets the short name to show for a type, without its namespace or generic arity.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to name.</param>
    /// <returns>The short name.</returns>
    internal static string ShortNameOf(Type type)
    {
        var name = type.Name;
        var arity = name.IndexOf('`');
        return arity < 0 ? name : name[..arity];
    }

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
