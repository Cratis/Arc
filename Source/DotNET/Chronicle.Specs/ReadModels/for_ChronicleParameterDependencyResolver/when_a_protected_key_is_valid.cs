// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_a_protected_key_is_valid : given.a_keyed_protected_read
{
    [Theory]
    [InlineData("owner")]
    [InlineData("a*b")]
    [InlineData("two words")]
    public void should_acquire_and_release_the_requested_model(string key)
    {
        var instance = new Balance(new(key));
        SelectKey(new AccountId(key));
        StubRead(key, instance);

        Read(false).ShouldBeSame(instance);

        _inner.Received(1).GetFor<Balance>(_unit, (ReadModelKey)key, Arg.Any<CancellationToken>());
        _stored.DidNotReceiveWithAnyArgs().GetInstanceById(default!, default!);
    }
}
