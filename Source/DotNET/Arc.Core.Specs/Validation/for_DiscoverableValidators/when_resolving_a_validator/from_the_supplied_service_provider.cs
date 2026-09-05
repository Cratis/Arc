// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using FluentValidationResult = FluentValidation.Results.ValidationResult;

namespace Cratis.Arc.Validation.for_DiscoverableValidators.when_resolving_a_validator;

public class from_the_supplied_service_provider : Specification
{
    DiscoverableValidators _validators;
    IValidator _validatorFromAllowingProvider = null!;
    IValidator _validatorFromRejectingProvider = null!;
    FluentValidationResult _allowingResult = null!;
    FluentValidationResult _rejectingResult = null!;

    void Establish()
    {
        _validators = new DiscoverableValidators(Cratis.Types.Types.Instance);

        var allowingProvider = new ServiceCollection()
            .AddSingleton(new CommandDependency { IsAllowed = true })
            .AddTransient<CommandWithDependencyValidator>()
            .BuildServiceProvider();

        var rejectingProvider = new ServiceCollection()
            .AddSingleton(new CommandDependency { IsAllowed = false })
            .AddTransient<CommandWithDependencyValidator>()
            .BuildServiceProvider();

        _validators.TryGet(typeof(CommandWithDependency), allowingProvider, out _validatorFromAllowingProvider!);
        _validators.TryGet(typeof(CommandWithDependency), rejectingProvider, out _validatorFromRejectingProvider!);
    }

    void Because()
    {
        var command = new CommandWithDependency(Guid.NewGuid());
        _allowingResult = _validatorFromAllowingProvider.Validate(new ValidationContext<CommandWithDependency>(command));
        _rejectingResult = _validatorFromRejectingProvider.Validate(new ValidationContext<CommandWithDependency>(command));
    }

    [Fact] void should_resolve_a_validator_whose_dependency_came_from_the_allowing_provider() => _allowingResult.IsValid.ShouldBeTrue();
    [Fact] void should_resolve_a_validator_whose_dependency_came_from_the_rejecting_provider() => _rejectingResult.IsValid.ShouldBeFalse();
}
