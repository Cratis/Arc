// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandResultFactories.when_checking_whether_reflection_can_be_used;

public class and_the_code_is_ahead_of_time_compiled : Specification
{
    bool _result;

    void Because() => _result = CommandResultFactories.CanCreateThroughReflection(isDynamicCodeSupported: false, isDynamicCodeCompiled: false);

    [Fact] void should_not_allow_reflection() => _result.ShouldBeFalse();
}
