// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions;

public class when_registrations_change_after_the_first_binding : given.a_decision_token_ownership_probe
{
    string _first;
    string _again;
    string _other;

    async Task Because()
    {
        await using var root = BuildPlain();
        await using var scope = root.CreateAsyncScope();
        _first = await Bind(scope.ServiceProvider);
        _services.Add(_override);
        _again = await Bind(scope.ServiceProvider);
        await using var other = BuildPlain();
        await using var otherScope = other.CreateAsyncScope();
        _other = await Bind(otherScope.ServiceProvider);
    }

    [Fact] void should_first_see_the_framework_owner() => _first.ShouldEqual(Framework);
    [Fact] void should_keep_the_first_answer_for_the_same_root() => _again.ShouldEqual(Framework);
    [Fact] void should_give_a_later_root_its_own_answer() => _other.ShouldEqual(Application);
}
