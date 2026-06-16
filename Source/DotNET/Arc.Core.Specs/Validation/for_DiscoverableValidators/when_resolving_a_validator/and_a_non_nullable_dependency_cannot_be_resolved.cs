// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Validation.for_DiscoverableValidators.when_resolving_a_validator;

public class and_a_non_nullable_dependency_cannot_be_resolved : Specification
{
    DiscoverableValidators _validators;
    ServiceProvider _provider = null!;
    Exception? _exception;

    void Establish()
    {
        _validators = new DiscoverableValidators(Cratis.Types.Types.Instance);

        _provider = new ServiceCollection()
            .AddScoped<CommandDependency>(_ => null!)
            .BuildServiceProvider();
    }

    void Because() =>
        _exception = Catch.Exception(() => _validators.TryGet(typeof(CommandWithDependency), _provider, out _));

    void Destroy() => _provider.Dispose();

    [Fact] void should_fail_with_a_clear_named_error() => _exception.ShouldBeOfExactType<CannotResolveValidatorDependency>();
    [Fact] void should_explain_that_dependency_could_not_be_resolved_or_resolved_to_null() => _exception!.Message.ShouldContain("could not be resolved or resolved to null");
}
