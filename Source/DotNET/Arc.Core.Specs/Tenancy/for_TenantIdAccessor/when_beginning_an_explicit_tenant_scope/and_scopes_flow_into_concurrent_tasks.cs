// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenantIdAccessor.when_beginning_an_explicit_tenant_scope;

public class and_scopes_flow_into_concurrent_tasks : given.a_tenant_id_accessor
{
    TenantId _firstAfterOtherDisposes;
    TenantId _secondAfterFirstDisposes;
    TenantId _after;

    void Establish() => _tenantIdResolver.Resolve().Returns("request-tenant");

    async Task Because()
    {
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstDisposed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await Task.WhenAll(First(), Second());
        _after = _accessor.Current;

        async Task First()
        {
            using (_accessor.Begin("acme"))
            {
                firstStarted.SetResult();
                await secondStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
                _firstAfterOtherDisposes = _accessor.Current;
            }

            firstDisposed.SetResult();
        }

        async Task Second()
        {
            using (_accessor.Begin("globex"))
            {
                secondStarted.SetResult();
                await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
                await firstDisposed.Task.WaitAsync(TimeSpan.FromSeconds(10));
                _secondAfterFirstDisposes = _accessor.Current;
            }
        }
    }

    [Fact] void should_keep_the_first_flow_selected() => _firstAfterOtherDisposes.ShouldEqual(new TenantId("acme"));
    [Fact] void should_keep_the_second_flow_selected() => _secondAfterFirstDisposes.ShouldEqual(new TenantId("globex"));
    [Fact] void should_not_select_a_tenant_in_the_calling_flow() => _after.ShouldEqual(new TenantId("request-tenant"));
}
