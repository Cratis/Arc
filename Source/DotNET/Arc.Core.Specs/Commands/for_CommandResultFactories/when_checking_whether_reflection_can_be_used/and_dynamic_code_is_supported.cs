// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandResultFactories.when_checking_whether_reflection_can_be_used;

public class and_dynamic_code_is_supported : Specification
{
    bool _result;

    void Because() => _result = CommandResultFactories.CanCreateThroughReflection(isDynamicCodeSupported: true, isDynamicCodeCompiled: true);

    [Fact] void should_allow_reflection() => _result.ShouldBeTrue();
}
