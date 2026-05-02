// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_SubjectValuesProvider.when_providing;

public class without_subject : Specification
{
    SubjectValuesProvider _provider;
    CommandContextValues _result;
    TestCommand _command;

    void Establish()
    {
        _provider = new SubjectValuesProvider();
        _command = new TestCommand();
    }

    void Because() => _result = _provider.Provide(_command);

    [Fact] void should_return_empty_values() => _result.ShouldBeEmpty();

    record TestCommand;
}
