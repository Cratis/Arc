// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandProvideInvoker.when_invoking;

public class and_provide_returns_a_tuple : given.a_command_provide_invoker
{
    class Command
    {
        public (string First, int Second) Provide() => ("hello", 42);
    }

    IReadOnlyList<object> _result;

    async Task Because() => _result = await Invoke(new Command());

    [Fact] void should_return_each_element() => _result.Count.ShouldEqual(2);
    [Fact] void should_contain_the_first_element() => _result.ShouldContain("hello");
    [Fact] void should_contain_the_second_element() => _result.ShouldContain(42);
}
