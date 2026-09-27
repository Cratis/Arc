// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Cratis.Arc.Queries.ModelBound;

[ReadModel]
public record QueryCandidateReadModel(int Id)
{
    public static QueryCandidateReadModel ById(int id)
    {
        static QueryCandidateReadModel Local(int value) => new(value);
        return new[] { Local(id) }.Select(model => model with { Id = id }).Single();
    }

    [CompilerGenerated]
    public static QueryCandidateReadModel Generated() => new(0);
    private static QueryCandidateReadModel PrivateHelper() => new(0);
    internal static QueryCandidateReadModel InternalHelper() => new(0);
    public static QueryCandidateReadModel Default => new(0);
    public static string WrongReturn() => string.Empty;
}
