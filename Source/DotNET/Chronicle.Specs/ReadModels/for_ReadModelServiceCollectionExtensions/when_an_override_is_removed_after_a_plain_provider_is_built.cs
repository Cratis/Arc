// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions;

public class when_an_override_is_removed_after_a_plain_provider_is_built : given.a_decision_token_ownership_probe
{
    string _owner;

    void Establish() => _services.Add(_override);

    async Task Because()
    {
        await using var root = BuildPlain();
        _services.Remove(_override);
        await using var scope = root.CreateAsyncScope();
        _owner = await Bind(scope.ServiceProvider);
    }

    [Fact] void should_judge_ownership_from_the_live_collection() => _owner.ShouldEqual(Framework);
    [Fact] void should_bypass_the_override_still_registered_in_the_built_provider() => _applicationCalls.ShouldEqual(0);
}
