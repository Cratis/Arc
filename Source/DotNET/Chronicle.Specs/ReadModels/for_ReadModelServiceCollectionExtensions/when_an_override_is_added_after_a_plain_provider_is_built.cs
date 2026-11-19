// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions;

public class when_an_override_is_added_after_a_plain_provider_is_built : given.a_decision_token_ownership_probe
{
    string _owner;

    async Task Because()
    {
        await using var root = BuildPlain();
        _services.Add(_override);
        await using var scope = root.CreateAsyncScope();
        _owner = await Bind(scope.ServiceProvider);
    }

    [Fact] void should_judge_ownership_from_the_live_collection() => _owner.ShouldEqual(ContainerFactory);
    [Fact] void should_not_run_the_application_registration_the_built_provider_never_saw() => _applicationCalls.ShouldEqual(0);
}
