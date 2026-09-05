// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.for_CommandHandlerArgumentResolver.when_resolving;

public class and_a_dependency_factory_throws_a_validation_failure : given.a_command_handler_argument_resolver
{
    class Dependency;

    class Handler
    {
        public void Handle(Dependency dependency) { }
    }

    Exception _exception;
    ServiceProvider _provider;

    void Establish()
    {
        HandleHasParameters<Handler>();
        ProvideReturns();

        // A read model factory that throws because the command carried no usable event source id is the real shape of
        // this: a registered service whose factory throws an IValidationFailure. Prove it propagates unwrapped through
        // GetService and the resolver, so the pipeline can convert it into a 400 rather than a 500.
        _provider = new ServiceCollection()
            .AddScoped<Dependency>(_ => throw new TheValidationFailure())
            .BuildServiceProvider();
        _serviceProvider = _provider;
    }

    async Task Because() => _exception = await Catch.Exception(async () => await Resolve());

    void Destroy() => _provider.Dispose();

    [Fact] void should_propagate_the_validation_failure_unwrapped() => (_exception is IValidationFailure).ShouldBeTrue();

    class TheValidationFailure : Exception, IValidationFailure
    {
        public ValidationResult ValidationResult { get; } = ValidationResult.Error("missing identifier");
    }
}
