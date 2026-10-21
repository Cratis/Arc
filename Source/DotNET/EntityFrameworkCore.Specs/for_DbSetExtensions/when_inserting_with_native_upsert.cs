// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.for_DbSetExtensions.Model;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.for_DbSetExtensions;

public class when_inserting_with_native_upsert : given.an_upsert_database
{
    UpsertItem _stored;

    async Task Because()
    {
        await _context.Items.UpsertAsync(_item);
        _stored = await _context.Items.AsNoTracking().SingleAsync();
    }

    [Fact] void should_insert_without_save_changes() => _stored.Id.ShouldEqual(_item.Id);
    [Fact] void should_apply_the_value_converter_and_parameterize_the_value() => _stored.Value.ShouldEqual(_item.Value);
    [Fact] void should_not_attach_the_entity() => _context.Entry(_item).State.ShouldEqual(EntityState.Detached);
}
