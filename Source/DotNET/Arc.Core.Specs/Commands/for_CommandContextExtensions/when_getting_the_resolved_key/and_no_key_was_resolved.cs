// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Commands.for_CommandContextExtensions.when_getting_the_resolved_key;

public class and_no_key_was_resolved : Specification
{
    string? _result;

    void Because() => _result = new CommandContext(CorrelationId.New(), typeof(object), new object(), [], new CommandContextValues()).GetResolvedKey();

    [Fact] void should_resolve_nothing() => _result.ShouldBeNull();
}
