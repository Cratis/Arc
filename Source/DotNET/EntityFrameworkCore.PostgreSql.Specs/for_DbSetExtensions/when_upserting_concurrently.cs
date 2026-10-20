// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.for_DbSetExtensions.Model;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.PostgreSql.for_DbSetExtensions;

[Collection(PostgreSqlCollection.Name)]
public class when_upserting_concurrently(PostgreSqlFixture fixture) : given.an_upsert_database(fixture)
{
    int _count;

    async Task Because()
    {
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var remaining = 8;
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var writes = Enumerable.Range(0, 8).Select(index => Task.Run(async () =>
        {
            await using var context = new UpsertDbContext(_options);
            if (Interlocked.Decrement(ref remaining) == 0) ready.SetResult();
            await start.Task;
            await context.Items.UpsertAsync(new UpsertItem { Tenant = _item.Tenant, Id = _item.Id, Value = new($"writer-{index}") });
        })).ToArray();
        await ready.Task.WaitAsync(TimeSpan.FromSeconds(20));
        start.SetResult();
        await Task.WhenAll(writes);
        _count = await _context.Items.CountAsync();
    }

    [Fact] void should_leave_one_row_without_duplicate_key_failures() => _count.ShouldEqual(1);
}
