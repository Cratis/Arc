// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandProvideInvoker.when_invoking;

public class and_provide_returns_a_single_value : given.a_command_provide_invoker
{
    class Command
    {
        public string Provide() => "the value";
    }

    IReadOnlyList<object> _result;

    async Task Because() => _result = await Invoke(new Command());

    [Fact] void should_return_a_single_value() => _result.Count.ShouldEqual(1);
    [Fact] void should_return_the_value() => _result[0].ShouldEqual("the value");
}
