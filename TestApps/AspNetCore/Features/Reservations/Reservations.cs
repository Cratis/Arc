// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Concepts;

namespace AspNetCore.Features.Reservations;

/// <summary>
/// Represents the reference a reservation is known by.
/// </summary>
/// <param name="Value">The underlying value.</param>
/// <remarks>
/// The separator in the name is deliberate. A Screenplay identifier is <c>[A-Za-z_]\w*</c>, so every name reaching a
/// document is reduced to one - once where the concept is declared and once everywhere the concept is referenced. A
/// reduction that stops agreeing with itself writes a document referring to a type it never declares, and only a name
/// that is really written apart asks that question of it.
/// </remarks>
public record Reservation_Reference(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Converts the reference to its underlying value.
    /// </summary>
    /// <param name="reference">The reference to convert.</param>
    public static implicit operator string(Reservation_Reference reference) => reference.Value;

    /// <summary>
    /// Converts an underlying value to a reference.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator Reservation_Reference(string value) => new(value);
}

/// <summary>
/// Represents a command reserving a table.
/// </summary>
/// <param name="Reference">The reference the reservation is known by.</param>
/// <param name="Description">What the reservation is for, in the words of the guest.</param>
/// <param name="Concurrency">How many tables are held at once.</param>
/// <param name="Guest">The name of the guest.</param>
/// <remarks>
/// <see cref="Description"/> and <see cref="Concurrency"/> carry the names they do on purpose. Screenplay is line
/// based and the body of a <c>command</c> decides what a line is from its first word, so a property written as
/// <c>description</c> or as <c>concurrency</c> is read as the directive of that name rather than as a property, and
/// the document is rejected. Both are left out and reported instead, which is what this slice holds the check to.
/// </remarks>
[Command, AllowAnonymous]
public record ReserveTable(Reservation_Reference Reference, string Description, int Concurrency, string Guest)
{
    /// <summary>
    /// Handles the command by holding the tables.
    /// </summary>
    public void Handle() =>
        Console.WriteLine($"Holding {Concurrency} table(s) for {Guest} under {Reference.Value}: {Description}");
}
