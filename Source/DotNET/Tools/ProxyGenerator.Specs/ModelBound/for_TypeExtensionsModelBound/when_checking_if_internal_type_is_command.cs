// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.ModelBound;

namespace Cratis.Arc.ProxyGenerator.Specs.ModelBound.for_TypeExtensionsModelBound;

public class when_checking_if_internal_type_is_command : Specification
{
    bool _result;

    void Because() => _result = typeof(Scenarios.for_Commands.ModelBound.InternalCommand).IsCommand();

    [Fact] void should_recognize_internal_command() => _result.ShouldBeTrue();
}
