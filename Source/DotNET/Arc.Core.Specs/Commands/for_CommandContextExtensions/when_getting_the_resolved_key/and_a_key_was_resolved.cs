// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Commands.for_CommandContextExtensions.when_getting_the_resolved_key;

public class and_a_key_was_resolved : Specification
{
    string? _result;

    void Because() => _result = ContextWith(new CommandContextValues { { CommandContextKeys.ResolvedKey, "resolved" } }).GetResolvedKey();

    [Fact] void should_read_it_back() => _result.ShouldEqual("resolved");

    static CommandContext ContextWith(CommandContextValues values) =>
        new(CorrelationId.New(), typeof(object), new object(), [], values);
}
