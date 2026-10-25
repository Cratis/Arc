// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.for_DbSetExtensions;

public class when_upserting_with_a_cancelled_token : given.an_upsert_database
{
    Exception? _exception;

    async Task Because() => _exception = await Catch.Exception(() => _context.Items.UpsertAsync(_item, new CancellationToken(true)));

    [Fact] void should_honor_cancellation() => _exception.ShouldBeOfExactType<OperationCanceledException>();
    [Fact] async Task should_not_insert() => (await _context.Items.CountAsync(global::Xunit.TestContext.Current.CancellationToken)).ShouldEqual(0);
}
