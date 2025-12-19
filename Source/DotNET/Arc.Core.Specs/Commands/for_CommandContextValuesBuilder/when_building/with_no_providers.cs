// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandContextValuesBuilder.when_building;

public class with_no_providers : given.a_command_context_values_builder
{
    CommandContextValues _result;

    void Establish() => _providers.GetEnumerator().Returns(new List<ICommandContextValuesProvider>().GetEnumerator());

    void Because() => _result = _builder.Build(_command);

    [Fact] void should_return_empty_command_context_values() => _result.ShouldNotBeNull();
    [Fact] void should_return_values_with_no_entries() => _result.ShouldBeEmpty();
}