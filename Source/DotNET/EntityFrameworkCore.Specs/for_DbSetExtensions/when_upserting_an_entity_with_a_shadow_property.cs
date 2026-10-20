// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.for_DbSetExtensions.Model;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.for_DbSetExtensions;

public class when_upserting_an_entity_with_a_shadow_property : given.an_upsert_database
{
    ShadowPropertyItem _replacement;
    int _trackedCount;
    string _valueBeforeSave;
    string? _storedExtra;
    string _storedValue;

    async Task Establish()
    {
        var original = new ShadowPropertyItem { Id = Guid.NewGuid(), Value = "original" };
        _context.ShadowItems.Add(original);
        _context.Entry(original).Property("Extra").CurrentValue = "preserved";
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        _replacement = new ShadowPropertyItem { Id = original.Id, Value = "updated" };
    }

    async Task Because()
    {
        await _context.ShadowItems.UpsertAsync(_replacement);
        _trackedCount = _context.ChangeTracker.Entries<ShadowPropertyItem>().Count();
        _valueBeforeSave = await _context.ShadowItems.AsNoTracking().Select(_ => _.Value).SingleAsync();
        await _context.SaveChangesAsync();
        await using var verification = new UpsertDbContext(_options);
        _storedExtra = await verification.ShadowItems.Select(_ => EF.Property<string?>(_, "Extra")).SingleAsync();
        _storedValue = await verification.ShadowItems.Select(_ => _.Value).SingleAsync();
    }

    [Fact] void should_fall_back_to_tracking_the_existing_entity() => _trackedCount.ShouldEqual(1);
    [Fact] void should_defer_the_update_until_save_changes() => _valueBeforeSave.ShouldEqual("original");
    [Fact] void should_preserve_the_shadow_property() => _storedExtra.ShouldEqual("preserved");
    [Fact] void should_update_the_scalar_property() => _storedValue.ShouldEqual("updated");
}
