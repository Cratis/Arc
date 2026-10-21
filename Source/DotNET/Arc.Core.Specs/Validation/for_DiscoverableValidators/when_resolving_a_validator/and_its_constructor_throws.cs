// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Validation.for_DiscoverableValidators.when_resolving_a_validator;

/// <summary>
/// Keeps the established construction of an unregistered validator outside read protection: a constructor failure
/// surfaces exactly as it did before read protection existed.
/// </summary>
public class and_its_constructor_throws : Specification
{
    ServiceProvider _provider = null!;
    Exception? _error;

    void Establish() => _provider = new ServiceCollection().AddSingleton(new Dependency()).BuildServiceProvider();

    void Because()
    {
        var types = Substitute.For<ITypes>();
        types.FindMultiple(typeof(IDiscoverableValidator<>)).Returns([typeof(ThrowingValidator)]);
        _error = Catch.Exception(() => new DiscoverableValidators(types).TryGet(typeof(Model), _provider, out _));
    }

    void Destroy() => _provider.Dispose();

    [Fact] void should_wrap_the_failure_as_reflection_reports_it() => _error.ShouldBeOfExactType<TargetInvocationException>();
    [Fact] void should_keep_the_constructor_failure() => _error!.InnerException.ShouldBeOfExactType<InvalidOperationException>();

    public record Model;
    public class Dependency;
    public class ThrowingValidator : AbstractValidator<Model>, IDiscoverableValidator<Model>
    {
        public ThrowingValidator(Dependency dependency) => throw new InvalidOperationException(dependency.ToString());
    }
}
