// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.for_DbSetExtensions.Model;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.for_DbSetExtensions;

public class when_upserting_a_generated_key : given.an_upsert_database
{
    GeneratedKeyItem _generated;
    EntityState _state;
    int _countBeforeSave;

    async Task Because()
    {
        _generated = new GeneratedKeyItem { Value = "generated" };
        await _context.Generated.UpsertAsync(_generated);
        _state = _context.Entry(_generated).State;
        _countBeforeSave = await _context.Generated.CountAsync();
        await _context.SaveChangesAsync();
    }

    [Fact] void should_use_the_tracked_fallback() => _state.ShouldEqual(EntityState.Added);
    [Fact] void should_require_save_changes() => _countBeforeSave.ShouldEqual(0);
    [Fact] void should_populate_the_generated_key() => _generated.Id.ShouldBeGreaterThan(0);
}
