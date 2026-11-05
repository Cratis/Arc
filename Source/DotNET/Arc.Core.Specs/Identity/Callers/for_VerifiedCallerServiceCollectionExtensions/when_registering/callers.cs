// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Cratis.Arc.Tenancy;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerServiceCollectionExtensions.when_registering;

public class callers : Specification
{
    ServiceCollection _services;
    CallerResolution _resolution;
    bool _sameInScope;
    bool _differentAcrossScopes;

    async Task Because()
    {
        _services = new();
        _services.AddCratisArcCore();
        _services.AddSingleton(Substitute.For<IHttpRequestContextAccessor>());
        _services.AddSingleton(Substitute.For<ITenantIdAccessor>());
        await using var provider = _services.BuildServiceProvider();
        await using var first = provider.CreateAsyncScope();
        await using var second = provider.CreateAsyncScope();
        var accessor = first.ServiceProvider.GetRequiredService<IVerifiedCallerAccessor>();
        _sameInScope = ReferenceEquals(accessor, first.ServiceProvider.GetRequiredService<IVerifiedCallerAccessor>());
        _differentAcrossScopes = !ReferenceEquals(accessor, second.ServiceProvider.GetRequiredService<IVerifiedCallerAccessor>());
        _resolution = await accessor.GetCurrent();
    }

    [Fact] void should_register_a_scoped_accessor() => _sameInScope.ShouldBeTrue();
    [Fact] void should_not_share_accessors_between_scopes() => _differentAcrossScopes.ShouldBeTrue();
    [Fact] void should_not_register_a_production_source() => _services.Any(descriptor => descriptor.ServiceType == typeof(IVerifiedCredentialSource)).ShouldBeFalse();
    [Fact] void should_remain_inert_without_a_request() => _resolution.Caller.ShouldBeNull();
}
