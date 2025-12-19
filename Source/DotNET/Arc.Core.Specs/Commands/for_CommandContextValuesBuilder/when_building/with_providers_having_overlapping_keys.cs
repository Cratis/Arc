// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandContextValuesBuilder.when_building;

public class with_providers_having_overlapping_keys : given.a_command_context_values_builder
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
            ["key1"] = "first_value",
            ["key2"] = 42
        };

        _secondProviderValues = new CommandContextValues
        {
            ["key1"] = "second_value",
            ["key3"] = true
        };

        _firstProvider.Provide(Arg.Any<object>()).Returns(_firstProviderValues);
        _secondProvider.Provide(Arg.Any<object>()).Returns(_secondProviderValues);
        _providers.GetEnumerator().Returns(new List<ICommandContextValuesProvider> { _firstProvider, _secondProvider }.GetEnumerator());
    }

    void Because() => _result = _builder.Build(_command);

    [Fact] void should_contain_all_unique_keys() => _result.Keys.ShouldContain("key1", "key2", "key3");
    [Fact] void should_have_value_from_last_provider_for_overlapping_key() => _result["key1"].ShouldEqual("second_value");
    [Fact] void should_have_value_from_first_provider_for_non_overlapping_key() => _result["key2"].ShouldEqual(42);
    [Fact] void should_have_value_from_second_provider_for_unique_key() => _result["key3"].ShouldEqual(true);
    [Fact] void should_have_three_entries() => _result.Count.ShouldEqual(3);
}