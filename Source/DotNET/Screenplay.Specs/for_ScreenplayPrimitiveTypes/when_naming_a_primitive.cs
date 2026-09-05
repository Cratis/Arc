// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Types;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ScreenplayPrimitiveTypes;

/// <summary>
/// Screenplay keeps distinctions the TypeScript proxy type system collapses - <c>Int</c> is not <c>Decimal</c> and
/// <c>Date</c> is not <c>DateTime</c> - so the leaf map cannot be shared with the proxy generator.
/// </summary>
public class when_naming_a_primitive : Specification
{
    [Fact] void should_name_a_uuid() => ScreenplayPrimitiveTypes.GetName(ScreenplayPrimitive.Uuid).ShouldEqual("Uuid");
    [Fact] void should_name_a_string() => ScreenplayPrimitiveTypes.GetName(ScreenplayPrimitive.String).ShouldEqual("String");
    [Fact] void should_name_an_int() => ScreenplayPrimitiveTypes.GetName(ScreenplayPrimitive.Int).ShouldEqual("Int");
    [Fact] void should_name_a_decimal() => ScreenplayPrimitiveTypes.GetName(ScreenplayPrimitive.Decimal).ShouldEqual("Decimal");
    [Fact] void should_name_a_bool() => ScreenplayPrimitiveTypes.GetName(ScreenplayPrimitive.Bool).ShouldEqual("Bool");
    [Fact] void should_name_a_date() => ScreenplayPrimitiveTypes.GetName(ScreenplayPrimitive.Date).ShouldEqual("Date");
    [Fact] void should_name_a_date_time() => ScreenplayPrimitiveTypes.GetName(ScreenplayPrimitive.DateTime).ShouldEqual("DateTime");
    [Fact] void should_name_an_enum() => ScreenplayPrimitiveTypes.GetName(ScreenplayPrimitive.Enum).ShouldEqual("Enum");
}
