// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandProvideInvoker.when_invoking;

public class and_provide_throws : given.a_command_provide_invoker
{
    class Command
    {
        public string Provide() => throw new Exception("provide failed");
    }

    Exception _exception;

    async Task Because() => _exception = await Catch.Exception(async () => await Invoke(new Command()));

    [Fact] void should_surface_the_inner_exception() => _exception.ShouldNotBeNull();
    [Fact] void should_preserve_the_message() => _exception.Message.ShouldEqual("provide failed");
}
