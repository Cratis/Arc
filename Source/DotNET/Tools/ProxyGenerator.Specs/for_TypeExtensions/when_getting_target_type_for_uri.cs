// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions;

public class when_getting_target_type_for_uri : Specification
{
    TargetType _result = null!;

    void Because() => _result = typeof(Uri).GetTargetType();

    [Fact] void should_map_to_the_string_type() => _result.Type.ShouldEqual("string");
    [Fact] void should_construct_with_the_string_constructor() => _result.Constructor.ShouldEqual("String");
}
