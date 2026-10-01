// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc;

/// <summary>
/// The span, attribute, event and metric names Arc's telemetry uses, for building queries, dashboards and alerts.
/// </summary>
/// <remarks>
/// Every attribute carries a type, a name, an identifier or an outcome - never a value taken from a command, query or
/// event payload. A payload value can be personal data, and a telemetry backend keeps it long after the application
/// has forgotten it.
/// </remarks>
public static class WellKnownTelemetryNames
{
    /// <summary>The span raised when a command runs through the pipeline.</summary>
    public const string CommandExecuteSpan = "cratis.arc.command.execute";

    /// <summary>The span raised when a command is validated without being run.</summary>
    public const string CommandValidateSpan = "cratis.arc.command.validate";

    /// <summary>The span raised when the command filters run.</summary>
    public const string CommandFilterSpan = "cratis.arc.command.filter";

    /// <summary>The span raised when an authorization filter decides on a command.</summary>
    public const string CommandAuthorizeSpan = "cratis.arc.command.authorize";

    /// <summary>The span raised when a model-bound command's <c>Provide()</c> method runs.</summary>
    public const string CommandProvideSpan = "cratis.arc.command.provide";

    /// <summary>The span raised when a command handler runs.</summary>
    public const string CommandHandleSpan = "cratis.arc.command.handle";

    /// <summary>The span raised when a validator runs.</summary>
    public const string ValidatorInvokeSpan = "cratis.arc.validator.invoke";

    /// <summary>The span raised when a query runs through the pipeline.</summary>
    public const string QueryPerformSpan = "cratis.arc.query.perform";

    /// <summary>The attribute holding the full name of the command type.</summary>
    public const string CommandType = "cratis.arc.command.type";

    /// <summary>The attribute holding the outcome of a command, one of <see cref="WellKnownOperationOutcomes"/>.</summary>
    public const string CommandOutcome = "cratis.arc.command.outcome";

    /// <summary>The attribute holding the type the command declares its key as, such as its event source id.</summary>
    public const string CommandKeyType = "cratis.arc.command.key.type";

    /// <summary>The attribute holding the fully qualified name of the query.</summary>
    public const string QueryName = "cratis.arc.query.name";

    /// <summary>The attribute holding the transport a query result is delivered over.</summary>
    public const string QueryTransport = "cratis.arc.query.transport";

    /// <summary>The attribute holding the outcome of a query, one of <see cref="WellKnownOperationOutcomes"/>.</summary>
    public const string QueryOutcome = "cratis.arc.query.outcome";

    /// <summary>The attribute holding the full name of a validator type.</summary>
    public const string ValidatorType = "cratis.arc.validator.type";

    /// <summary>The attribute holding the number of results a validator produced.</summary>
    public const string ValidationResultCount = "cratis.arc.validation.result_count";

    /// <summary>The attribute holding the resolved tenant.</summary>
    public const string Tenant = "cratis.tenant";

    /// <summary>The attribute holding the correlation id.</summary>
    public const string CorrelationId = "cratis.correlation_id";

    /// <summary>The event raised for each validation result that blocked an operation.</summary>
    public const string ValidationFailedEvent = "cratis.arc.validation.failed";

    /// <summary>The event attribute holding the severity of a validation result.</summary>
    public const string ValidationSeverity = "cratis.arc.validation.severity";

    /// <summary>The event attribute holding the members a validation result is for.</summary>
    public const string ValidationMembers = "cratis.arc.validation.members";

    /// <summary>The event attribute holding what composed a validation result.</summary>
    public const string ValidationReason = "cratis.arc.validation.reason";

    /// <summary>The event attribute holding the identity of what composed a validation result, such as a constraint name.</summary>
    public const string ValidationReasonDetail = "cratis.arc.validation.reason_detail";

    /// <summary>The event raised when authorization denied an operation.</summary>
    public const string AuthorizationDeniedEvent = "cratis.arc.authorization.denied";

    /// <summary>The OpenTelemetry semantic convention event for an exception.</summary>
    public const string ExceptionEvent = "exception";

    /// <summary>The OpenTelemetry semantic convention attribute for the type of an exception.</summary>
    public const string ExceptionType = "exception.type";

    /// <summary>The histogram recording how long a command took, in seconds.</summary>
    public const string CommandDurationMetric = "cratis.arc.command.duration";

    /// <summary>The counter recording command outcomes.</summary>
    public const string CommandOutcomesMetric = "cratis.arc.command.outcomes";

    /// <summary>The histogram recording how long a query took, in seconds.</summary>
    public const string QueryDurationMetric = "cratis.arc.query.duration";

    /// <summary>The value recorded for a command type or query name past the cardinality limit, or for a query that is not known.</summary>
    public const string Other = "_other";

    /// <summary>The transport recorded for a query that failed before it ran.</summary>
    public const string UnknownTransport = "unknown";

    /// <summary>The transport of a query answered once.</summary>
    public const string SnapshotTransport = "snapshot";

    /// <summary>The transport of a query that keeps pushing changes.</summary>
    public const string ObservableTransport = "observable";
}
