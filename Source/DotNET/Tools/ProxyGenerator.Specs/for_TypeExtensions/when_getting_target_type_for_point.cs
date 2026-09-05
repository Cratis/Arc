// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Geospatial;

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions;

public class when_getting_target_type_for_point : Specification
{
    TargetType _result = null!;

    void Because() => _result = typeof(Point).GetTargetType();

    [Fact] void should_have_the_point_type_name() => _result.Type.ShouldEqual("Point");
    [Fact] void should_import_from_the_fundamentals_package() => _result.Module.ShouldEqual("@cratis/fundamentals");
    [Fact] void should_be_from_package() => _result.FromPackage.ShouldBeTrue();
}
