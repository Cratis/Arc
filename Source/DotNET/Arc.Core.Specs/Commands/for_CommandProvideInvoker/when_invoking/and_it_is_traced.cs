// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandProvideInvoker.when_invoking;

public class and_it_is_traced : given.a_traced_command_provide_invoker
{
    public class RegisterAuthor
    {
        public string Provide() => "the value";
    }

    async Task Because() => await Invoke(new RegisterAuthor());

    [Fact] void should_keep_the_stable_span_name() => ProvideSpan.DisplayName.ShouldEqual(WellKnownTelemetryNames.CommandProvideSpan);
    [Fact] void should_add_the_command_type() => ProvideSpan.GetTagItem(WellKnownTelemetryNames.CommandType).ShouldEqual(typeof(RegisterAuthor).FullName);
}
