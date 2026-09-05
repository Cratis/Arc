// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Validation.for_DiscoverableValidators.when_resolving_a_validator;

/// <summary>
/// Proves the reason read models (registered as scoped, keyed off the command's event source id) could not be
/// injected into a validator when the validator was resolved from the root provider: a scoped dependency cannot be
/// resolved from the root provider, but it resolves cleanly from the command scope.
/// </summary>
public class with_a_scoped_dependency : Specification
{
    DiscoverableValidators _validators;
    ServiceProvider _root = null!;
    bool _resolvedFromCommandScope;
    Exception _exceptionResolvingFromRoot = null!;

    void Establish()
    {
        _validators = new DiscoverableValidators(Cratis.Types.Types.Instance);
        _root = new ServiceCollection()
            .AddScoped(_ => new CommandDependency { IsAllowed = true })
            .AddTransient<CommandWithDependencyValidator>()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    void Because()
    {
        using var commandScope = _root.CreateScope();
        _resolvedFromCommandScope = _validators.TryGet(typeof(CommandWithDependency), commandScope.ServiceProvider, out _);
        _exceptionResolvingFromRoot = Catch.Exception(() => _validators.TryGet(typeof(CommandWithDependency), _root, out _));
    }

    void Destroy() => _root.Dispose();

    [Fact] void should_resolve_the_validator_from_the_command_scope() => _resolvedFromCommandScope.ShouldBeTrue();
    [Fact] void should_fail_to_resolve_the_scoped_dependency_from_the_root_provider() => _exceptionResolvingFromRoot.ShouldNotBeNull();
}
