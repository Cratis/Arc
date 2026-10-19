// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Validation.for_DiscoverableValidators.when_using_a_certified_provider;

public class with_unrecorded_reader_bindings : given.a_certified_validator
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void should_refuse_an_unrecorded_reader_anywhere_without_resolving_any_dependency(bool first, bool factory)
    {
        var calls = 0;
        var descriptor = factory
            ? ServiceDescriptor.Scoped<IReader>(_ =>
            {
                calls++;
                return new Reader(_events);
            })
            : ServiceDescriptor.Singleton<IReader>(new Reader(_events));
        _services.Insert(first ? 0 : _services.Count, descriptor);
        Build();
        Catch.Exception(() => _validators.TryGet(typeof(Model), _scope.ServiceProvider, out _))
            .ShouldBeOfExactType<DiscoverableValidatorRefusedInProtectedDecision>();
        calls.ShouldEqual(0);
        _events.ShouldBeEmpty();
    }
}
