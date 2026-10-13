// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandRefusalExtensions.when_classifying;

public class with_authorization_and_an_exception : given.a_command_result
{
    void Establish()
    {
        _result.IsAuthorized = false;
        _result.ExceptionMessages = ["unsupported policy"];
    }
    void Because() => _refusal = _result.GetRefusal();

    [Fact] void should_not_acknowledge_a_non_refusal_failure() => _refusal.ShouldBeNull();
}
