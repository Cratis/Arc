// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using FluentValidationResult = FluentValidation.Results.ValidationResult;

namespace Cratis.Arc.Validation.for_DiscoverableValidators.when_resolving_a_validator;

public class and_a_nullable_dependency_resolves_to_null : Specification
{
    readonly ServiceProvider _provider;
    readonly bool _resolved;
    readonly IValidator _validator;
    FluentValidationResult _result;

    public and_a_nullable_dependency_resolves_to_null()
    {
        var validators = new DiscoverableValidators(Cratis.Types.Types.Instance);

        _provider = new ServiceCollection()
            .AddScoped<CommandDependency>(_ => null!)
            .BuildServiceProvider();

        _resolved = validators.TryGet(typeof(CommandWithNullableDependency), _provider, out var validator);
        _validator = validator;
    }

    void Because() =>
        _result = _validator.Validate(new ValidationContext<CommandWithNullableDependency>(new CommandWithNullableDependency(Guid.NewGuid())));

    void Destroy() => _provider.Dispose();

    [Fact] void should_resolve_the_validator() => _resolved.ShouldBeTrue();
    [Fact] void should_construct_the_validator_with_null_injected() => _validator.ShouldNotBeNull();
    [Fact] void should_run_the_validator_and_reject_the_missing_dependency() => _result.IsValid.ShouldBeFalse();
}
