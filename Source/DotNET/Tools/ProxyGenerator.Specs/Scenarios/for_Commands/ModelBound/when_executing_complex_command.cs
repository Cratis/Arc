// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ModelBound;

public class when_executing_complex_command : given.a_scenario_web_application
{
    CommandResult<ComplexCommandResult>? _result;

    void Establish() => LoadCommandProxy<ComplexCommand>();

    async Task Because()
    {
        var executionResult = await Bridge.ExecuteCommandViaProxyAsync<ComplexCommandResult>(new ComplexCommand
        {
            Nested = new NestedType
            {
                Name = "NestedName",
                Child = new NestedChild
                {
                    Id = Guid.NewGuid(),
                    Value = 123.45
                }
            },
            Items = ["item1", "item2", "item3"],
            Values = new Dictionary<string, int>
            {
                ["key1"] = 1,
                ["key2"] = 2
            },
            Timeout = TimeSpan.FromMinutes(30)
        });

        _result = executionResult.Result;
    }

    [Fact] void should_return_successful_result() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_response() => _result.Response.ShouldNotBeNull();
    [Fact] void should_have_received_nested_name() => _result.Response.ReceivedNested.ShouldEqual("NestedName");
    [Fact] void should_have_correct_item_count() => _result.Response.ItemCount.ShouldEqual(3);
    [Fact] void should_have_correct_value_count() => _result.Response.ValueCount.ShouldEqual(2);
    [Fact] void should_have_correct_timeout() => _result.Response.ReceivedTimeout.ShouldEqual(TimeSpan.FromMinutes(30));
}
