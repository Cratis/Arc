// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents paging for a query.
/// </summary>
public record Paging
{
    /// <summary>
    /// Represents a not paged result.
    /// </summary>
    public static readonly Paging NotPaged = new(0, 0, false);

    /// <summary>
    /// Initializes a new instance of the <see cref="Paging"/> class.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <param name="size">The size of a page.</param>
    /// <param name="isPaged">Whether or not paging is to be used.</param>
    public Paging(PageNumber page, PageSize size, bool isPaged)
    {
        Page = page;
        Size = size;
        IsPaged = isPaged;
    }

    /// <summary>
    /// The page number.
    /// </summary>
    public PageNumber Page { get; }

    /// <summary>
    /// The size of a page.
    /// </summary>
    public PageSize Size { get; }

    /// <summary>
    /// Whether paging is to be used.
    /// </summary>
    public bool IsPaged { get; }

    /// <summary>
    /// Gets the number of items to skip.
    /// </summary>
    /// <remarks>
    /// The multiplication is performed in 64-bit and clamped to the non-negative <see cref="int"/> range: a hostile or
    /// malformed page/size (for example <c>page=999999999</c> with a very large page size) would otherwise overflow the
    /// 32-bit product into a negative value and surface as a server error when passed to the underlying data source.
    /// </remarks>
    public int Skip => (int)Math.Clamp((long)Page.Value * Size.Value, 0L, int.MaxValue);
}
