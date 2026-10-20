// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.for_DbSetExtensions.Model;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.for_DbSetExtensions;

public class when_upserting_a_reference_navigation_with_a_shadow_foreign_key : given.an_upsert_database
{
    UpsertBlog _blog;
    UpsertPost _post;
    int _trackedCount;
    string _titleBeforeSave;
    string _storedTitle;
    Guid? _storedBlogId;

    async Task Establish()
    {
        _blog = new UpsertBlog { Id = Guid.NewGuid() };
        var original = new UpsertPost { Id = Guid.NewGuid(), Title = "original", Blog = _blog };
        _context.Posts.Add(original);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        _post = new UpsertPost { Id = original.Id, Title = "updated", Blog = _blog };
    }

    async Task Because()
    {
        await _context.Posts.UpsertAsync(_post);
        _trackedCount = _context.ChangeTracker.Entries<UpsertPost>().Count();
        _titleBeforeSave = await _context.Posts.AsNoTracking().Select(_ => _.Title).SingleAsync();
        await _context.SaveChangesAsync();
        await using var verification = new UpsertDbContext(_options);
        _storedBlogId = await verification.Posts.Select(_ => EF.Property<Guid?>(_, "BlogId")).SingleAsync();
        _storedTitle = await verification.Posts.Select(_ => _.Title).SingleAsync();
    }

    [Fact] void should_fall_back_to_tracking_the_existing_post() => _trackedCount.ShouldEqual(1);
    [Fact] void should_defer_the_update_until_save_changes() => _titleBeforeSave.ShouldEqual("original");
    [Fact] void should_preserve_the_shadow_foreign_key() => _storedBlogId.ShouldEqual((Guid?)_blog.Id);
    [Fact] void should_update_the_scalar_property() => _storedTitle.ShouldEqual("updated");
}
