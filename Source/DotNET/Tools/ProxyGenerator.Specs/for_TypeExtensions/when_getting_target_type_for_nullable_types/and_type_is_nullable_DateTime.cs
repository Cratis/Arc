// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_getting_target_type_for_nullable_types;

public class and_type_is_nullable_DateTime : Specification
{
    TargetType _result = null!;

    void Because() => _result = typeof(DateTime?).GetTargetType();

    [Fact] void should_have_date_as_type() => _result.Type.ShouldEqual("Date");
    [Fact] void should_have_date_as_constructor() => _result.Constructor.ShouldEqual("Date");
}
