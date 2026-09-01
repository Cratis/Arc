// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Marks a command, or a single property on one, as something whose value must never be written to the causation
/// chain.
/// </summary>
/// <remarks>
/// <para>
/// A command's property values are recorded on the causation of every event it appends, so that an event says not
/// only which command produced it but what that command was asked to do. Some values must not travel that way. A
/// password, a token, an API key or a card number would be written into the event log in plain text and stay there
/// for as long as the events do - immutably, and read by everything that ever replays them.
/// </para>
/// <para>
/// Personal data does not need this attribute: a property covered by Chronicle's
/// <see cref="Cratis.Chronicle.Compliance.GDPR.PIIAttribute"/> is already left out. Use this one for the values that
/// are sensitive without being personal, which is exactly the set GDPR does not describe.
/// </para>
/// <para>
/// Applied to the command type it excludes every property at once, which is the right answer when a command exists
/// only to carry secrets. The command is still named on the chain either way - what is withheld is the values, never
/// the fact that the command ran.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [Command]
/// public record ChangePassword(
///     UserId User,
///     [property: NotAudited] string OldPassword,
///     [property: NotAudited] string NewPassword);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class NotAuditedAttribute : Attribute;
