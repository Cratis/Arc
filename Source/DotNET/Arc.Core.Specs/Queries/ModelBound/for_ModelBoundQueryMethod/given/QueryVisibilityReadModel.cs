// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryMethod.given;

[ReadModel]
public class QueryVisibilityReadModel
{
    public static QueryVisibilityReadModel Public() => new();
    internal static QueryVisibilityReadModel Internal() => new();
    static QueryVisibilityReadModel Private() => new();
    protected static QueryVisibilityReadModel Protected() => new();
    protected internal static QueryVisibilityReadModel ProtectedInternal() => new();
    private protected static QueryVisibilityReadModel PrivateProtected() => new();
}
