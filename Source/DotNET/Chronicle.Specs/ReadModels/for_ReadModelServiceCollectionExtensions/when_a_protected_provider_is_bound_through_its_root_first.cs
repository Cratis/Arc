// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions;

public class when_a_protected_provider_is_bound_through_its_root_first : given.a_decision_token_ownership_probe
{
    string _root;
    string _child;

    async Task Because()
    {
        await using var root = BuildProtected();
        _services.Add(_override);
        _root = await Bind(root);
        await using var scope = root.CreateAsyncScope();
        _child = await Bind(scope.ServiceProvider);
    }

    [Fact] void should_read_the_live_collection_for_the_root() => _root.ShouldEqual(ContainerFactory);
    [Fact] void should_share_the_cached_answer_with_later_child_scopes() => _child.ShouldEqual(ContainerFactory);
}
