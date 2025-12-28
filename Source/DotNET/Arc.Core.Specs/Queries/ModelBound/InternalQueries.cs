// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type

namespace Cratis.Arc.Queries.ModelBound;

/// <summary>
/// Internal read model class for testing internal query support.
/// </summary>
[ReadModel]
internal class InternalReadModel
{
    public Guid Id { get; set; }
    public string Value { get; set; } = string.Empty;

    internal static InternalReadModel Query(Guid id)
    {
        return new InternalReadModel { Id = id, Value = "Internal query result" };
    }
}

/// <summary>
/// Public read model with internal query method for testing.
/// </summary>
[ReadModel]
public class PublicReadModelWithInternalQuery
{
    public Guid Id { get; set; }
    public string Value { get; set; } = string.Empty;

    internal static PublicReadModelWithInternalQuery Query(Guid id)
    {
        return new PublicReadModelWithInternalQuery { Id = id, Value = "Internal query result" };
    }
}
