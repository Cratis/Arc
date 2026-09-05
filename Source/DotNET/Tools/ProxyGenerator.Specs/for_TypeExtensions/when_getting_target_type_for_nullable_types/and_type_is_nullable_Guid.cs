// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_getting_target_type_for_nullable_types;

public class and_type_is_nullable_Guid : Specification
{
    TargetType _result = null!;

    void Because() => _result = typeof(Guid?).GetTargetType();

    [Fact] void should_have_guid_as_type() => _result.Type.ShouldEqual("Guid");
    [Fact] void should_have_guid_as_constructor() => _result.Constructor.ShouldEqual("Guid");
    [Fact] void should_have_cratis_fundamentals_as_module() => _result.Module.ShouldEqual("@cratis/fundamentals");
    [Fact] void should_be_from_package() => _result.FromPackage.ShouldBeTrue();
}
