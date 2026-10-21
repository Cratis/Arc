// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.for_DbSetExtensions.Model;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.for_DbSetExtensions;

public class when_using_the_tracked_upsert : given.an_upsert_database
{
    EntityState _insertState;
    int _countBeforeSave;
    UpsertItem _stored;

    async Task Establish()
    {
        await _context.Items.Upsert(_item);
        _insertState = _context.Entry(_item).State;
        _countBeforeSave = await _context.Items.CountAsync();
        await _context.SaveChangesAsync();
    }

    async Task Because()
    {
        await _context.Items.Upsert(new UpsertItem { Tenant = _item.Tenant, Id = _item.Id, Value = new("tracked update") });
        await _context.SaveChangesAsync();
        _stored = await _context.Items.AsNoTracking().SingleAsync();
    }

    [Fact] void should_keep_the_original_insert_tracked() => _insertState.ShouldEqual(EntityState.Added);
    [Fact] void should_not_insert_until_save_changes() => _countBeforeSave.ShouldEqual(0);
    [Fact] void should_update_an_already_tracked_instance() => _stored.Value.Value.ShouldEqual("tracked update");
}
