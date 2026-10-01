// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Arc.Commands.for_CommandProvideInvoker.when_invoking;

public class and_it_is_traced_and_provide_throws : given.a_traced_command_provide_invoker
{
    public class RegisterAuthor
    {
        public string Provide() => throw new InvalidOperationException("the store is gone");
    }

    async Task Because() => await Catch.Exception(async () => await Invoke(new RegisterAuthor()));

    [Fact] void should_set_the_status_to_error() => ProvideSpan.Status.ShouldEqual(ActivityStatusCode.Error);
    [Fact] void should_describe_the_status_as_an_error() => ProvideSpan.StatusDescription.ShouldEqual(WellKnownOperationOutcomes.Error);
}
