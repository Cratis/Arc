// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Validation;

/// <summary>
/// Represents what composed a <see cref="ValidationResult"/> - the machine-readable counterpart to its message.
/// </summary>
/// <param name="Value">The reason identifier.</param>
/// <remarks>
/// A rejection's message is a developer diagnostic, not end-user copy, and it is the only thing that has ever
/// distinguished one kind of rejection from another. That leaves a client matching English prose to tell an
/// authored business-rule rejection from something the framework composed on its behalf - which breaks silently the
/// moment the wording changes, and cannot tell the framework's sentence from an authored rule that reads the same.
/// <para>
/// This is deliberately an open set rather than an enum. Rejections are composed in Arc, in Chronicle and in
/// consumer code, and a closed set would make every new kind a breaking change for whoever switches over it.
/// </para>
/// </remarks>
public record ValidationResultReason(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// A rule authored by the application rejected the input. The default: the message is the author's, and the
    /// client should show it.
    /// </summary>
    public static readonly ValidationResultReason Rule = new("rule");

    /// <summary>
    /// The append was rejected because the event source moved on since it was read. The failure is retryable -
    /// re-read and resubmit - which is the fact a free-text message cannot convey.
    /// </summary>
    public static readonly ValidationResultReason ConcurrencyViolation = new("concurrencyViolation");

    /// <summary>
    /// A constraint on the event store rejected the append.
    /// </summary>
    public static readonly ValidationResultReason ConstraintViolation = new("constraintViolation");

    /// <summary>
    /// A validator threw while validating, and the framework substituted a result for the one the validator would
    /// have produced. Nothing about the authored rules survives, so a client that shows the message shows a
    /// developer diagnostic in place of an authored reason.
    /// </summary>
    public static readonly ValidationResultReason ValidatorFailed = new("validatorFailed");

    /// <summary>
    /// Something the command needed in order to be evaluated was not available - most often the read model a
    /// validator depends on, which could not be resolved from the command's key.
    /// </summary>
    /// <remarks>
    /// This is raised <em>before</em> any rule runs, so no rule rejected anything: the command was never evaluated
    /// against them at all. That is a different fact from a rule rejection and wants a different response, and it is
    /// the one most easily mistaken for a domain rejection because the message reads like one.
    /// </remarks>
    public static readonly ValidationResultReason DependencyUnavailable = new("dependencyUnavailable");

    /// <summary>
    /// The request itself could not be read - a malformed body, or a value that could not be bound to the member it
    /// was sent for. No rule was reached.
    /// </summary>
    public static readonly ValidationResultReason MalformedRequest = new("malformedRequest");
}
