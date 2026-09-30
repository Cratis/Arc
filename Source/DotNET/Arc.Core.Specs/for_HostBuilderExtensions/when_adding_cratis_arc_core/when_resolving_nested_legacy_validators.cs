// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.for_HostBuilderExtensions.when_adding_cratis_arc_core;

#pragma warning disable SA1402, SA1649

public class when_resolving_nested_legacy_validators
{
    [Fact]
    public void should_keep_convention_self_bindings_for_a_parent_validator_injecting_a_child()
    {
        using var provider = new ServiceCollection().AddLogging().AddCratisArcCore().BuildServiceProvider();
        var parent = provider.GetRequiredService<ParentValidator>();
        Assert.NotNull(parent.Child);
        Assert.NotNull(provider.GetRequiredService<ChildValidator>());
        var discovery = provider.GetRequiredService<IDiscoverableValidators>();
        Assert.True(discovery.TryGet(typeof(Parent), provider, out var resolved));
        Assert.NotNull(Assert.IsType<ParentValidator>(resolved).Child);
    }

    public record Parent;
    public record Child;

    public class ChildValidator : CommandValidator<Child>;

    public class ParentValidator(ChildValidator child) : CommandValidator<Parent>
    {
        public ChildValidator Child { get; } = child;
    }
}

#pragma warning restore SA1402, SA1649
