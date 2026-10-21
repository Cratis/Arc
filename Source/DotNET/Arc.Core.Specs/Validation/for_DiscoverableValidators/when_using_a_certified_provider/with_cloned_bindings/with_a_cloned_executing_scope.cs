// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Validation.for_DiscoverableValidators.when_using_a_certified_provider.with_cloned_bindings;

public class with_a_cloned_executing_scope : given.a_certified_validator
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task should_refuse_from_original_and_cloned_graphs_without_any_activation(bool certifiedClone, bool factoryOverride)
    {
        Build();
        IServiceCollection copied = new ServiceCollection();
        foreach (var binding in _services) copied.Add(binding);
        var factoryCalls = 0;
        if (factoryOverride)
        {
            copied.AddTransient<ReadingValidator>(_ =>
            {
                factoryCalls++;
                throw new InvalidOperationException("copied factory");
            });
        }
        await using var clone = (ServiceProvider)(certifiedClone ? copied.BuildProtectedDecisionServiceProvider() : copied.BuildServiceProvider());
        await using var scope = clone.CreateAsyncScope();
        foreach (var validators in new[] { _validators, new DiscoverableValidators(_types) })
        {
            var graph = new ModelGraphValidator(validators, new ValidatorInvoker(NullLogger<ValidatorInvoker>.Instance));
            var error = await Catch.Exception(() => graph.Validate(new(new Model(), scope.ServiceProvider)));
            error.ShouldBeOfExactType<DiscoverableValidatorRefusedInProtectedDecision>();
        }
        factoryCalls.ShouldEqual(0);
        _events.ShouldBeEmpty();
    }
}
