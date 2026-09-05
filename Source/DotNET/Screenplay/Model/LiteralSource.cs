// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents a constant value.
/// </summary>
/// <param name="Value">The value - <see langword="null"/>, a <see cref="bool"/>, a <see cref="string"/>, a number or an <see cref="EnumValue"/>.</param>
/// <remarks>
/// Every number reaching the printer has to be a <see cref="double"/>, because anything else is formatted with the
/// current culture. Emission converts numeric values, so any numeric type may be carried here.
/// <para>
/// A member of an enumeration is carried as an <see cref="EnumValue"/> rather than as the number behind it, because
/// the number says nothing about which member of the concept it names.
/// </para>
/// </remarks>
public record LiteralSource(object? Value) : MappingSourceModel;
