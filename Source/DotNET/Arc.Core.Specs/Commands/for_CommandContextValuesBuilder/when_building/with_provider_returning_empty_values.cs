// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandContextValuesBuilder.when_building;

public class with_provider_returning_empty_values : given.a_command_context_values_builder
{
    CommandContextValues _result;
    ICommandContextValuesProvider _provider;
    CommandContextValues _emptyValues;

    void Establish()
    {
        _provider = Substitute.For<ICommandContextValuesProvider>();
        _emptyValues = new CommandContextValues();
        _provider.Provide(Arg.Any<object>()).Returns(_emptyValues);
        _providers.GetEnumerator().Returns(new List<ICommandContextValuesProvider> { _provider }.GetEnumerator());
    }

    void Because() => _result = _builder.Build(_command);

    [Fact] void should_return_empty_command_context_values() => _result.ShouldBeEmpty();
    [Fact] void should_not_be_null() => _result.ShouldNotBeNull();
}