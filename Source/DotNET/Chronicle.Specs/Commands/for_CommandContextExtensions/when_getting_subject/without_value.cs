// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;

namespace Cratis.Arc.Chronicle.Commands.for_CommandContextExtensions.when_getting_subject;

public class without_value : given.a_command_context
{
    Subject? _result;

    void Because() => _result = _commandContext.GetSubject();

    [Fact] void should_return_null() => _result.ShouldBeNull();
}
