// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Commands.for_CommandDecisionPolicy;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Validation.for_DiscoverableValidators.when_resolving_a_validator.in_a_protected_decision.given;

public class a_protected_decision : Specification
{
    protected DiscoverableValidators _validators;
    protected IServiceCollection _services;
    protected ServiceProvider? _provider;

    void Establish()
    {
        _validators = new DiscoverableValidators(Cratis.Types.Types.Instance);
        _services = new ServiceCollection();
    }

    void Destroy() => _provider?.Dispose();

    /// <summary>
    /// Resolves a validator inside a protected decision. The policy is AsyncLocal, so resolution runs in its flow.
    /// </summary>
    /// <param name="modelType">The model type to resolve a validator for.</param>
    /// <returns>The resolved validator, or the exception that refused it.</returns>
    protected (object? Validator, Exception? Error) ResolveInProtectedDecision(Type modelType)
    {
        _provider ??= _services.BuildServiceProvider();
        using var decision = CommandDecisionPolicy.Begin(typeof(ProtectedCommand));
        object? validator = null;
        var error = Record.Exception(() =>
        {
            _validators.TryGet(modelType, _provider, out var resolved);
            validator = resolved;
        });
        return (validator, error);
    }
}
