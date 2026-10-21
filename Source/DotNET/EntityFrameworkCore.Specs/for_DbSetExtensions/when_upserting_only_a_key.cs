// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.for_DbSetExtensions.Model;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.for_DbSetExtensions;

public class when_upserting_only_a_key : given.an_upsert_database
{
    int _count;

    async Task Because()
    {
        await _context.Keys.UpsertAsync(new KeyOnlyItem { Id = "same-key" });
        await _context.Keys.UpsertAsync(new KeyOnlyItem { Id = "same-key" });
        _count = await _context.Keys.CountAsync();
    }

    [Fact] void should_allow_a_retry_without_an_update_clause() => _count.ShouldEqual(1);
}
