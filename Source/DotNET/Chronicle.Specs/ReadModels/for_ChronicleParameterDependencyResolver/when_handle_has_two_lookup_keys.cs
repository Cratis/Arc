// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_handle_has_two_lookup_keys : given.a_parameter_lookup
{
    void Establish() => Configure(new Transfer(new("from"), new("to")));
    async Task Because() => await ResolveHandle();
    [Fact] void should_inject_the_from_model() => _arguments.Arguments[0].ShouldBeSame(_from);
    [Fact] void should_inject_the_to_model() => _arguments.Arguments[1].ShouldBeSame(_to);
}
