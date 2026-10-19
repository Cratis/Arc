// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Cratis.Arc.DependencyInjection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Validation.for_DiscoverableValidators.when_using_a_certified_provider;

public class with_unsupported_graphs : given.a_certified_validator
{
    [Theory]
    [InlineData(typeof(CollectionValidator))]
    [InlineData(typeof(ProviderValidator))]
    [InlineData(typeof(FactoryValidator))]
    [InlineData(typeof(CyclicValidator))]
    [InlineData(typeof(AmbiguousValidator))]
    [InlineData(typeof(KeyedValidator))]
    [InlineData(typeof(OptionalValidator))]
    public void should_refuse_the_entire_graph_before_resolving_reads_or_constructing_any_branch(Type validator)
    {
        ProtectedDecisionRegistrationOrigins.For(_services).DiscoverValidators([validator]);
        _services.AddTransient<Cycle>();
        _services.AddTransient<LateOpaqueValidator>();
        Build();
        Catch.Exception(() => CertifiedProtectedValidatorConstruction.Construct(_scope.ServiceProvider, validator, true))
            .ShouldBeOfExactType<DiscoverableValidatorRefusedInProtectedDecision>();
        _events.ShouldBeEmpty();
    }

    public class CollectionValidator(IEnumerable<IReader> readers) : AbstractValidator<Model>
    {
        public IEnumerable Readers { get; } = readers;
    }
    public class ProviderValidator(IServiceProvider provider) : AbstractValidator<Model>
    {
        public IServiceProvider Provider { get; } = provider;
    }
    public class FactoryValidator(Func<IReader> factory) : AbstractValidator<Model>
    {
        public Func<IReader> Factory { get; } = factory;
    }
    public class Cycle(Cycle next)
    {
        public Cycle Next { get; } = next;
    }
    public class CyclicValidator(Cycle cycle) : AbstractValidator<Model>
    {
        public Cycle Cycle { get; } = cycle;
    }
    public class AmbiguousValidator : AbstractValidator<Model>
    {
        public AmbiguousValidator() { }
        public AmbiguousValidator(IReader reader) => Reader = reader;
        public IReader Reader { get; }
    }
    public class KeyedValidator([FromKeyedServices("reader")] IReader reader) : AbstractValidator<Model>
    {
        public IReader Reader { get; } = reader;
    }
    public class OptionalValidator(IReader? reader = null) : AbstractValidator<Model>
    {
        public IReader? Reader { get; } = reader;
    }
}
