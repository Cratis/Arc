// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenantIdAccessor.when_beginning_an_explicit_tenant_scope;

public class and_a_task_outlives_its_parent_scope : given.a_tenant_id_accessor
{
    TenantId _captured;
    TenantId _parent;

    async Task Because()
    {
        _tenantIdResolver.Resolve().Returns("resolved");
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<TenantId> task;
        using (_accessor.Begin("tenant-A"))
        {
            task = Task.Run(async () =>
            {
                await release.Task;
                return _accessor.Current;
            });
        }

        _parent = _accessor.Current;
        release.SetResult();
        _captured = await task;
    }

    [Fact] void should_keep_the_captured_tenant_in_the_task() => _captured.ShouldEqual(new TenantId("tenant-A"));
    [Fact] void should_restore_the_parent_tenant() => _parent.ShouldEqual(new TenantId("resolved"));
}
