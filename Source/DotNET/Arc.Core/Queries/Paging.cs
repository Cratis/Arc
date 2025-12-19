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
    public Paging(int page, int size, bool isPaged)
    {
        if (isPaged)
        {
            Validate(page, size);
        }
        Page = page;
        Size = size;
        IsPaged = isPaged;
    }

    /// <summary>
    /// The page number.
    /// </summary>
    public int Page { get; }

    /// <summary>
    /// The size of a page.
    /// </summary>
    public int Size { get; }

    /// <summary>
    /// Whether paging is to be used.
    /// </summary>
    public bool IsPaged { get; }

    /// <summary>
    /// Gets the number of items to skip.
    /// </summary>
    public int Skip => Page * Size;

    void Validate(int page, int size)
    {
        if (page < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(page), "Page number must be greater or equal to 0");
        }
        if (size <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Page size must be greater than 0");
        }
    }
}
