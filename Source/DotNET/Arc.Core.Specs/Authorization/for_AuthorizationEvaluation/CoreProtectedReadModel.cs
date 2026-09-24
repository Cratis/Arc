// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation;

[ReadModel]
[Authorize(Policy = "CorePermission")]
public record CoreProtectedReadModel(string Value)
{
    static int _performed;

    public static int Performed => Volatile.Read(ref _performed);

    public static CoreProtectedReadModel All()
    {
        Interlocked.Increment(ref _performed);
        return new("allowed");
    }
}
