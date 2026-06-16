// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using FluentValidationResult = FluentValidation.Results.ValidationResult;

namespace Cratis.Arc.Validation.for_DiscoverableValidators.when_resolving_a_validator;

public class and_a_nullable_dependency_resolves_to_an_instance : Specification
{
    readonly ServiceProvider _provider;
    readonly bool _resolved;
    readonly IValidator _validator;
    FluentValidationResult _result;

    public and_a_nullable_dependency_resolves_to_an_instance()
    {
        var validators = new DiscoverableValidators(Cratis.Types.Types.Instance);

        _provider = new ServiceCollection()
            .AddScoped(_ => new CommandDependency { IsAllowed = true })
            .BuildServiceProvider();

        _resolved = validators.TryGet(typeof(CommandWithNullableDependency), _provider, out var validator);
        _validator = validator;
    }

    void Because() =>
        _result = _validator.Validate(new ValidationContext<CommandWithNullableDependency>(new CommandWithNullableDependency(Guid.NewGuid())));

    void Destroy() => _provider.Dispose();

    [Fact] void should_resolve_the_validator() => _resolved.ShouldBeTrue();
    [Fact] void should_inject_the_resolved_instance_and_pass() => _result.IsValid.ShouldBeTrue();
}
