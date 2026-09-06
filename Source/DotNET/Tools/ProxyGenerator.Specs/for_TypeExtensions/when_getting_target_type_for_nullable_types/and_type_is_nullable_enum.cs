// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_getting_target_type_for_nullable_types;

public class and_type_is_nullable_enum : Specification
{
    enum SomeStatus
    {
        Unknown = 0,
        Active = 1
    }

    TargetType _result = null!;

    void Because() => _result = typeof(SomeStatus?).GetTargetType();

    [Fact] void should_have_the_enum_name_as_type() => _result.Type.ShouldEqual(nameof(SomeStatus));
    [Fact] void should_have_number_as_constructor() => _result.Constructor.ShouldEqual("Number");
}
