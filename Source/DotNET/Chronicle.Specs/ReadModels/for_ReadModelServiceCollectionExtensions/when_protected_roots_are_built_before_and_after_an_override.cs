// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions;

public class when_protected_roots_are_built_before_and_after_an_override : given.a_decision_token_ownership_probe
{
    string _original;
    string _changed;

    async Task Because()
    {
        await using var original = BuildProtected();
        _services.Add(_override);
        await using var changed = BuildProtected();
        await using var originalScope = original.CreateAsyncScope();
        await using var changedScope = changed.CreateAsyncScope();
        _original = await Bind(originalScope.ServiceProvider);
        _changed = await Bind(changedScope.ServiceProvider);
    }

    [Fact] void should_keep_the_original_root_framework_owned() => _original.ShouldEqual(Framework);
    [Fact] void should_make_the_changed_root_application_owned() => _changed.ShouldEqual(Application);
}
