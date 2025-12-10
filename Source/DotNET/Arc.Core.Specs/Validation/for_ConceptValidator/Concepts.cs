// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Arc.Validation.for_ConceptValidator;

#pragma warning disable SA1649, SA1402
public record StringConcept(string Value) : ConceptAs<string>(Value);
public record BoolConcept(bool Value) : ConceptAs<bool>(Value);
public record GuidConcept(Guid Value) : ConceptAs<Guid>(Value);
public record DateOnlyConcept(DateOnly Value) : ConceptAs<DateOnly>(Value);
public record TimeOnlyConcept(TimeOnly Value) : ConceptAs<TimeOnly>(Value);
public record DateTimeConcept(DateTime Value) : ConceptAs<DateTime>(Value);
public record DateTimeOffsetConcept(DateTimeOffset Value) : ConceptAs<DateTimeOffset>(Value);
public record FloatConcept(float Value) : ConceptAs<float>(Value);
public record DoubleConcept(double Value) : ConceptAs<double>(Value);
public record DecimalConcept(decimal Value) : ConceptAs<decimal>(Value);
public record SByteConcept(sbyte Value) : ConceptAs<sbyte>(Value);
public record ShortConcept(short Value) : ConceptAs<short>(Value);
public record IntConcept(int Value) : ConceptAs<int>(Value);
public record LongConcept(long Value) : ConceptAs<long>(Value);
public record ByteConcept(byte Value) : ConceptAs<byte>(Value);
public record UShortConcept(ushort Value) : ConceptAs<ushort>(Value);
public record UIntConcept(uint Value) : ConceptAs<uint>(Value);
public record ULongConcept(ulong Value) : ConceptAs<ulong>(Value);
#pragma warning restore SA1649, SA1402
