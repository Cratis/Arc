// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_SubjectValuesProvider.when_providing;

public class with_interface_implementation : Specification
{
    SubjectValuesProvider _provider;
    CommandContextValues _result;
    TestCommand _command;
    Subject _subject;

    void Establish()
    {
        _provider = new SubjectValuesProvider();
        _subject = new Subject("customer-77");
        _command = new TestCommand(_subject);
    }

    void Because() => _result = _provider.Provide(_command);

    [Fact] void should_return_subject_value() => _result.ContainsKey(WellKnownCommandContextKeys.Subject).ShouldBeTrue();
    [Fact] void should_have_correct_subject() => ((Subject)_result[WellKnownCommandContextKeys.Subject]).ShouldEqual(_subject);

    record TestCommand(Subject Subject) : ICanProvideSubject
    {
        public Subject GetSubject() => Subject;
    }
}
