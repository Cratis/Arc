// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.for_OperationSnapshot.when_beginning;

public class in_parallel_operations : Specification
{
    (string Subject, TenantId Tenant)[] _seen;

    async Task Because() => _seen = await Task.WhenAll(Enumerable.Range(0, 8).Select(index => Task.Run(async () =>
    {
        using var boundary = TenantIdAccessor.Independent.BeginRequest();
        var resolver = Substitute.For<ITenantIdResolver>();
        resolver.Resolve().Returns($"tenant-{index}");
        var principals = Substitute.For<ICurrentPrincipalAccessor>();
        principals.Current.Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", $"user-{index}")], "test")));
        var accessor = new TenantIdAccessor(resolver);
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(ICurrentPrincipalAccessor)).Returns(principals);
        services.GetService(typeof(TenantIdAccessor)).Returns(accessor);
        services.GetService(typeof(ITenantIdAccessor)).Returns(accessor);
        services.GetService(typeof(ITenantIdResolver)).Returns(resolver);
        using var snapshot = OperationSnapshot.Begin(services, () => null);
        await Task.Delay(20);
        return (snapshot.CausedBy.Subject.Value, snapshot.Tenant);
    })));

    [Fact] void should_give_each_operation_its_own_caller_and_tenant() =>
        _seen.Select((seen, index) => seen.Subject == $"user-{index}" && seen.Tenant == new TenantId($"tenant-{index}")).ShouldEachConformTo(_ => _);
}
