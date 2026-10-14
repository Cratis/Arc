// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_convention_is_off : given.a_parameter_lookup
{
    void Establish() => Configure(new Convention(new("from")));
    async Task Because() => await ResolveProvide();
    [Fact] void should_keep_the_command_key_lookup() => _provided[0].ShouldBeSame(_subject);
}
