// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_TypeExtensionsModelBound;

[ReadModel]
public record QueryCandidateReadModel(int Id)
{
    public static IEnumerable<QueryCandidateReadModel> All(int id)
    {
        static IEnumerable<QueryCandidateReadModel> Local() => [];
        return Local().Select(model => model with { Id = id });
    }

    private static IEnumerable<QueryCandidateReadModel> PrivateHelper() => [];
    internal static IEnumerable<QueryCandidateReadModel> InternalHelper() => [];
    [CompilerGenerated]
    public static QueryCandidateReadModel Generated() => new(0);
    public static QueryCandidateReadModel Default => new(0);
    public static IEnumerable<string> WrongCollection() => [];
    public static string WrongScalar() => string.Empty;
}

[ReadModel]
public record ReadModelWithOnlyLocalFunction(int Id)
{
    private static IEnumerable<ReadModelWithOnlyLocalFunction> Helper()
    {
        static IEnumerable<ReadModelWithOnlyLocalFunction> Local() => [];
        return Local();
    }
}
