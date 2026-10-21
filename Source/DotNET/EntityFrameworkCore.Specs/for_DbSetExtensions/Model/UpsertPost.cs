// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EntityFrameworkCore.for_DbSetExtensions.Model;

public class UpsertPost
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public UpsertBlog? Blog { get; set; }
}
