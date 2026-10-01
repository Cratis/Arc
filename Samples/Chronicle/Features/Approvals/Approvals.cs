// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Concepts;

namespace Chronicle.Features.Approvals;

/// <summary>
/// Represents how far an approval has come.
/// </summary>
/// <remarks>
/// The members carry values of their own so that the difference between the number the compiler hands over and the
/// name the concept declares it under is visible. A document setting a property to <c>6</c> is valid and untrue; one
/// setting it to <c>"declined"</c> says what the application says.
/// </remarks>
public enum ApprovalStatus
{
    /// <summary>Nobody has decided yet.</summary>
    Pending = 0,

    /// <summary>The approval was given.</summary>
    Granted = 3,

    /// <summary>The approval was refused.</summary>
    Declined = 6
}

/// <summary>
/// Represents a label an approval is classified by.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record ApprovalLabel(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Converts the label to its underlying value.
    /// </summary>
    /// <param name="label">The label to convert.</param>
    public static implicit operator string(ApprovalLabel label) => label.Value;

    /// <summary>
    /// Converts an underlying value to a label.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator ApprovalLabel(string value) => new(value);
}

/// <summary>
/// The event that occurs when an approval has been requested.
/// </summary>
/// <param name="Requester">Who asked for the approval.</param>
/// <param name="Tag">What the request is classified by.</param>
/// <remarks>
/// <see cref="Tag"/> carries the name it does on purpose. The body of an event declaration reads a line starting
/// with <c>tag</c> as a tag of its own, so a property of that name is swallowed by it rather than declared. It
/// holds several values so that the swallowed line would be one the language rejects outright - a property left in
/// by mistake is then a document that does not compile rather than a document that silently means something else.
/// </remarks>
[EventType]
public record ApprovalRequested(string Requester, IEnumerable<ApprovalLabel> Tag);

/// <summary>
/// The event that occurs when an approval has been decided.
/// </summary>
/// <param name="Approver">Who decided the approval.</param>
/// <param name="Outcome">What was decided.</param>
[EventType]
public record ApprovalDecided(string Approver, ApprovalStatus Outcome);

/// <summary>
/// Represents a command asking for an approval.
/// </summary>
/// <param name="ApprovalId">The approval being asked for.</param>
/// <param name="Requester">Who is asking.</param>
[Command]
public record RequestApproval(EventSourceId ApprovalId, string Requester)
{
    /// <summary>
    /// Handles the command by stating that the approval was asked for.
    /// </summary>
    /// <returns>The <see cref="ApprovalRequested"/> event.</returns>
    public ApprovalRequested Handle() => new(Requester, []);
}

/// <summary>
/// Represents a command deciding an approval.
/// </summary>
/// <param name="ApprovalId">The approval being decided.</param>
/// <param name="Approver">Who is deciding.</param>
/// <param name="Outcome">What is being decided.</param>
[Command]
public record DecideApproval(EventSourceId ApprovalId, string Approver, ApprovalStatus Outcome)
{
    /// <summary>
    /// Handles the command by stating what was decided.
    /// </summary>
    /// <returns>The <see cref="ApprovalDecided"/> event.</returns>
    public ApprovalDecided Handle() => new(Approver, Outcome);
}

/// <summary>
/// Represents the state of one approval.
/// </summary>
/// <param name="Requester">Who asked for the approval.</param>
/// <param name="Approver">Who decided it, empty while nobody has.</param>
/// <param name="Status">How far the approval has come.</param>
/// <remarks>
/// <see cref="Status"/> is set to a constant of an enumeration, which the compiler hands over as the number behind
/// the member. Writing that number is valid Screenplay and says something the application does not, so the member it
/// names is what the document has to carry.
/// </remarks>
[ReadModel]
public record Approval(
    [property: SetFrom<ApprovalRequested>(nameof(ApprovalRequested.Requester))] string Requester,
    [property: SetFrom<ApprovalDecided>(nameof(ApprovalDecided.Approver))] string Approver,
    [property: SetValue<ApprovalDecided>(ApprovalStatus.Granted)] ApprovalStatus Status);
