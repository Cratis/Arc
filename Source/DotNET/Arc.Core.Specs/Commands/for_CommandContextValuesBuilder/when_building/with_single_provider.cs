// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandContextValuesBuilder.when_building;

public class with_single_provider : given.a_command_context_values_builder
{
    CommandContextValues _result;
    ICommandContextValuesProvider _provider;
    CommandContextValues _providedValues;

    void Establish()
    {
        _provider = Substitute.For<ICommandContextValuesProvider>();
        _providedValues = new CommandContextValues
        {
            ["key1"] = "value1",
            ["key2"] = 42
        };
        _provider.Provide(Arg.Any<object>()).Returns(_providedValues);
        _providers.GetEnumerator().Returns(new List<ICommandContextValuesProvider> { _provider }.GetEnumerator());
    }

    void Because() => _result = _builder.Build(_command);

    [Fact] void should_return_command_context_values_with_provided_values() => _result.ShouldEqual(_providedValues);
    [Fact] void should_contain_all_keys_from_provider() => _result.Keys.ShouldContain("key1", "key2");
    [Fact]
    void should_contain_correct_values()
    {
        _result["key1"].ShouldEqual("value1");
        _result["key2"].ShouldEqual(42);
    }
}