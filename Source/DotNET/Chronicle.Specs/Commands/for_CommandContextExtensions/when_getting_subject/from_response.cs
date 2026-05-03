// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;

namespace Cratis.Arc.Chronicle.Commands.for_CommandContextExtensions.when_getting_subject;

public class from_response : given.a_command_context
{
    Subject _subject;
    Subject? _result;

    void Establish()
    {
        _subject = new Subject("customer-response");
        _commandContext = _commandContext with { Response = _subject };
    }

    void Because() => _result = _commandContext.GetSubject();

    [Fact] void should_return_the_subject_from_response() => _result.ShouldEqual(_subject);
}
