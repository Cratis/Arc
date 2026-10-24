// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.for_DbSetExtensions.Model;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.PostgreSql.for_DbSetExtensions;

[Collection(PostgreSqlCollection.Name)]
public class when_updating_with_native_upsert(PostgreSqlFixture fixture) : given.an_upsert_database(fixture)
{
    UpsertItem _stored;
    int _count;

    async Task Establish()
    {
        await _context.Items.UpsertAsync(_item);
        await _context.Items.UpsertAsync(new UpsertItem { Tenant = "another", Id = _item.Id, Value = new("untouched") });
        _item.Value = new("updated");
        _item.Optional = null;
    }

    async Task Because()
    {
        await _context.Items.UpsertAsync(_item);
        await _context.Items.UpsertAsync(_item);
        _stored = await _context.Items.AsNoTracking().SingleAsync(_ => _.Tenant == _item.Tenant);
        _count = await _context.Items.CountAsync();
    }

    [Fact] void should_update_the_existing_row() => _stored.Value.ShouldEqual(_item.Value);
    [Fact] void should_persist_null() => _stored.Optional.ShouldBeNull();
    [Fact] void should_match_the_entire_composite_key_and_allow_retries() => _count.ShouldEqual(2);
    [Fact] async Task should_leave_the_other_key_untouched() => (await _context.Items.SingleAsync(_ => _.Tenant == "another", global::Xunit.TestContext.Current.CancellationToken)).Value.Value.ShouldEqual("untouched");
}
