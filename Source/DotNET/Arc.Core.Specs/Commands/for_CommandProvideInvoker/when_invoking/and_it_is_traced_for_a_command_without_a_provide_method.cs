// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandProvideInvoker.when_invoking;

public class and_it_is_traced_for_a_command_without_a_provide_method : given.a_traced_command_provide_invoker
{
    public record RegisterAuthor(string Name);

    async Task Because() => await Invoke(new RegisterAuthor("a name"));

    [Fact] void should_not_raise_a_span() => _telemetry.Activities.ShouldBeEmpty();
}
