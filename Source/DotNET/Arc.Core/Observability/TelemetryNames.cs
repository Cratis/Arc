// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Observability;

/// <summary>
/// The attribute, event, metric and outcome names Arc's telemetry uses.
/// </summary>
/// <remarks>
/// Every attribute carries a type, a name, an identifier or an outcome - never a value taken from a command, query or
/// event payload. Keep it that way: a payload value can be personal data, and a telemetry backend keeps it long after
/// the application has forgotten it.
/// </remarks>
internal static class TelemetryNames
{
    /// <summary>The attribute holding the full name of the command type.</summary>
    internal const string CommandType = "cratis.arc.command.type";

    /// <summary>The attribute holding the outcome of a command.</summary>
    internal const string CommandOutcome = "cratis.arc.command.outcome";

    /// <summary>The attribute holding the full name of the type of the command's event source id.</summary>
    internal const string CommandEventSourceIdType = "cratis.arc.command.event_source_id.type";

    /// <summary>The attribute holding the fully qualified name of the query.</summary>
    internal const string QueryName = "cratis.arc.query.name";

    /// <summary>The attribute holding the transport a query result is delivered over.</summary>
    internal const string QueryTransport = "cratis.arc.query.transport";

    /// <summary>The attribute holding the outcome of a query.</summary>
    internal const string QueryOutcome = "cratis.arc.query.outcome";

    /// <summary>The attribute holding the full name of a validator type.</summary>
    internal const string ValidatorType = "cratis.arc.validator.type";

    /// <summary>The attribute holding the number of results a validator produced.</summary>
    internal const string ValidationResultCount = "cratis.arc.validation.result_count";

    /// <summary>The attribute holding the resolved tenant.</summary>
    internal const string Tenant = "cratis.tenant";

    /// <summary>The attribute holding the correlation id.</summary>
    internal const string CorrelationId = "cratis.correlation_id";

    /// <summary>The event raised for each validation result that blocked an operation.</summary>
    internal const string ValidationFailedEvent = "cratis.arc.validation.failed";

    /// <summary>The event attribute holding the severity of a validation result.</summary>
    internal const string ValidationSeverity = "cratis.arc.validation.severity";

    /// <summary>The event attribute holding the members a validation result is for.</summary>
    internal const string ValidationMembers = "cratis.arc.validation.members";

    /// <summary>The event attribute holding what composed a validation result.</summary>
    internal const string ValidationReason = "cratis.arc.validation.reason";

    /// <summary>The event attribute holding the identity of what composed a validation result, such as a constraint name.</summary>
    internal const string ValidationReasonDetail = "cratis.arc.validation.reason_detail";

    /// <summary>The event raised when authorization denied an operation.</summary>
    internal const string AuthorizationDeniedEvent = "cratis.arc.authorization.denied";

    /// <summary>The OpenTelemetry semantic convention event for an exception.</summary>
    internal const string ExceptionEvent = "exception";

    /// <summary>The OpenTelemetry semantic convention attribute for the type of an exception.</summary>
    internal const string ExceptionType = "exception.type";

    /// <summary>The histogram recording how long a command took, in seconds.</summary>
    internal const string CommandDurationMetric = "cratis.arc.command.duration";

    /// <summary>The counter recording command outcomes.</summary>
    internal const string CommandOutcomesMetric = "cratis.arc.command.outcomes";

    /// <summary>The histogram recording how long a query took, in seconds.</summary>
    internal const string QueryDurationMetric = "cratis.arc.query.duration";

    /// <summary>The value used for a type or name once the cardinality limit is reached, or when it is not known.</summary>
    internal const string Other = "_other";

    /// <summary>The value used for a query transport that is not known, typically because the query failed before it ran.</summary>
    internal const string Unknown = "unknown";

    /// <summary>The transport of a query answered once.</summary>
    internal const string Snapshot = "snapshot";

    /// <summary>The transport of a query that keeps pushing changes.</summary>
    internal const string Observable = "observable";
}
