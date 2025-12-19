// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandContextValuesBuilder.when_building;

public class with_multiple_providers : given.a_command_context_values_builder
{
    CommandContextValues _result;
    ICommandContextValuesProvider _firstProvider;
    ICommandContextValuesProvider _secondProvider;
    CommandContextValues _firstProviderValues;
    CommandContextValues _secondProviderValues;

    void Establish()
    {
        _firstProvider = Substitute.For<ICommandContextValuesProvider>();
        _secondProvider = Substitute.For<ICommandContextValuesProvider>();

        _firstProviderValues = new CommandContextValues
        {
            ["key1"] = "value1",
            ["key2"] = 42
        };

        _secondProviderValues = new CommandContextValues
        {
            ["key3"] = "value3",
            ["key4"] = true
        };

        _firstProvider.Provide(Arg.Any<object>()).Returns(_firstProviderValues);
        _secondProvider.Provide(Arg.Any<object>()).Returns(_secondProviderValues);
        _providers.GetEnumerator().Returns(new List<ICommandContextValuesProvider> { _firstProvider, _secondProvider }.GetEnumerator());
    }

    void Because() => _result = _builder.Build(_command);

    [Fact] void should_contain_all_keys_from_both_providers() => _result.Keys.ShouldContain("key1", "key2", "key3", "key4");
    [Fact]
    void should_contain_values_from_first_provider()
    {
        _result["key1"].ShouldEqual("value1");
        _result["key2"].ShouldEqual(42);
    }
    [Fact]
    void should_contain_values_from_second_provider()
    {
        _result["key3"].ShouldEqual("value3");
        _result["key4"].ShouldEqual(true);
    }
    [Fact] void should_have_four_entries() => _result.Count.ShouldEqual(4);
}