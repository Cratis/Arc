// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Types;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ScreenplayPrimitiveTypes;

/// <summary>
/// The map from framework type to primitive is what analysis resolves a concept's backing type with, and it keeps
/// distinctions that matter to a reader - a whole number is not a fractional one, a date is not a point in time.
/// </summary>
public class when_resolving_a_framework_type : Specification
{
    [Fact] void should_resolve_a_guid_to_uuid() => Resolve("System.Guid").ShouldEqual(ScreenplayPrimitive.Uuid);
    [Fact] void should_resolve_a_string_to_string() => Resolve("System.String").ShouldEqual(ScreenplayPrimitive.String);
    [Fact] void should_resolve_an_int_to_int() => Resolve("System.Int32").ShouldEqual(ScreenplayPrimitive.Int);
    [Fact] void should_resolve_a_long_to_int() => Resolve("System.Int64").ShouldEqual(ScreenplayPrimitive.Int);
    [Fact] void should_resolve_a_decimal_to_decimal() => Resolve("System.Decimal").ShouldEqual(ScreenplayPrimitive.Decimal);
    [Fact] void should_resolve_a_double_to_decimal() => Resolve("System.Double").ShouldEqual(ScreenplayPrimitive.Decimal);
    [Fact] void should_resolve_a_bool_to_bool() => Resolve("System.Boolean").ShouldEqual(ScreenplayPrimitive.Bool);
    [Fact] void should_resolve_a_date_only_to_date() => Resolve("System.DateOnly").ShouldEqual(ScreenplayPrimitive.Date);
    [Fact] void should_resolve_a_date_time_to_date_time() => Resolve("System.DateTime").ShouldEqual(ScreenplayPrimitive.DateTime);
    [Fact] void should_resolve_a_date_time_offset_to_date_time() => Resolve("System.DateTimeOffset").ShouldEqual(ScreenplayPrimitive.DateTime);
    [Fact] void should_not_resolve_a_type_of_the_application() => ScreenplayPrimitiveTypes.TryResolve("Library.AuthorId", out _).ShouldBeFalse();

    static ScreenplayPrimitive Resolve(string name)
    {
        ScreenplayPrimitiveTypes.TryResolve(name, out var primitive).ShouldBeTrue();

        return primitive;
    }
}
