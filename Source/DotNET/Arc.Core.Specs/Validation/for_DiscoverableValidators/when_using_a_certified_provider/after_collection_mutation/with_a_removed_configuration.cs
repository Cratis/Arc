// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.DependencyInjection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Validation.for_DiscoverableValidators.when_using_a_certified_provider.after_collection_mutation;

public class with_a_removed_configuration : given.a_certified_validator
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_keep_the_factory_and_denial_actually_built_in_the_original_root(bool warmed)
    {
        var callbacks = 0;
        _services.AddProtectedDecisionValidator<ReadingValidator>(validator =>
        {
            callbacks++;
            validator.RuleFor(model => model.Name).Must(_ => false).WithMessage("ownership denied");
        });
        Build();
        if (warmed) _validators.TryGet(typeof(Model), _scope.ServiceProvider, out _).ShouldBeTrue();
        _services.Remove(_services.Single(binding => binding.ServiceType == typeof(ReadingValidator)));
        var graph = new ModelGraphValidator(_validators, new ValidatorInvoker(NullLogger<ValidatorInvoker>.Instance));
        var result = (await graph.Validate(new(new Model(), _scope.ServiceProvider))).ToArray();
        result.Single().Message.ShouldEqual("ownership denied");
        callbacks.ShouldEqual(warmed ? 2 : 1);
        _services.AddTransient<ReadingValidator>(_ => throw new InvalidOperationException("late factory must not run"));
        result = (await graph.Validate(new(new Model(), _scope.ServiceProvider))).ToArray();
        result.Single().Message.ShouldEqual("ownership denied");
        callbacks.ShouldEqual(warmed ? 3 : 2);
    }
}
