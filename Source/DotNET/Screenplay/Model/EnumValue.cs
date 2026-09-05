// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents a constant that names a member of an enumeration.
/// </summary>
/// <param name="Member">The name of the member, in the casing the enumeration declares it.</param>
/// <remarks>
/// A constant of an enumeration arrives from the compiler as the number behind the member rather than as the member,
/// so a literal carrying the number alone refers to a value by a form the document never declares - the concept says
/// <c>clientContact</c> and the reference would read <c>6</c>. Naming the member is what lets emission write the form
/// the concept declares, while the model keeps to a name and stays free of anything the compiler knows.
/// </remarks>
public record EnumValue(string Member);
