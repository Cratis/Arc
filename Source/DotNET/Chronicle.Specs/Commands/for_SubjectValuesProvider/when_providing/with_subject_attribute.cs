// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle;

namespace Cratis.Arc.Chronicle.Commands.for_SubjectValuesProvider.when_providing;

public class with_subject_attribute : Specification
{
    SubjectValuesProvider _provider;
    CommandContextValues _result;
    TestCommand _command;

    void Establish()
    {
        _provider = new SubjectValuesProvider();
        _command = new TestCommand("customer-42");
    }

    void Because() => _result = _provider.Provide(_command);

    [Fact] void should_return_subject_value() => _result.ContainsKey(WellKnownCommandContextKeys.Subject).ShouldBeTrue();
    [Fact] void should_have_correct_subject() => ((Subject)_result[WellKnownCommandContextKeys.Subject]).Value.ShouldEqual("customer-42");

    record TestCommand([Subject] string CustomerId);
}
