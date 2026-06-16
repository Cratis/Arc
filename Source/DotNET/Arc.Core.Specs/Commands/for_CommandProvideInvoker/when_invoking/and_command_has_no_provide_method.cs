// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandProvideInvoker.when_invoking;

public class and_command_has_no_provide_method : given.a_command_provide_invoker
{
    class Command;

    IReadOnlyList<object> _result;

    async Task Because() => _result = await Invoke(new Command());

    [Fact] void should_return_no_values() => _result.ShouldBeEmpty();
}
