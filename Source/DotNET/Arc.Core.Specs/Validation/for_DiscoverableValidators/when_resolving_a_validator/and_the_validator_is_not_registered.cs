// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using FluentValidationResult = FluentValidation.Results.ValidationResult;

namespace Cratis.Arc.Validation.for_DiscoverableValidators.when_resolving_a_validator;

/// <summary>
/// Proves a validator resolves even when it is not registered in the container. The self-binding convention skips
/// validators with a record-typed constructor parameter (a Chronicle read model), so the validator must be
/// constructed on demand from the supplied provider, with its dependency resolved from that same provider — the
/// same way the command handler and Provide method resolve their dependencies.
/// </summary>
public class and_the_validator_is_not_registered : Specification
{
    DiscoverableValidators _validators;
    ServiceProvider _provider = null!;
    bool _resolved;
    IValidator _validator = null!;
    FluentValidationResult _result = null!;

    void Establish()
    {
        _validators = new DiscoverableValidators(Cratis.Types.Types.Instance);

        // The dependency is registered; the validator itself is deliberately NOT registered.
        _provider = new ServiceCollection()
            .AddSingleton(new CommandDependency { IsAllowed = false })
            .BuildServiceProvider();

        _resolved = _validators.TryGet(typeof(CommandWithDependency), _provider, out _validator!);
    }

    void Because() =>
        _result = _validator.Validate(new ValidationContext<CommandWithDependency>(new CommandWithDependency(Guid.NewGuid())));

    void Destroy() => _provider.Dispose();

    [Fact] void should_resolve_the_validator() => _resolved.ShouldBeTrue();
    [Fact] void should_construct_a_usable_validator() => _validator.ShouldNotBeNull();
    [Fact] void should_run_the_validator_against_the_resolved_dependency() => _result.IsValid.ShouldBeFalse();
}
