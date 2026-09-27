// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.Queries.for_QueryScenario;

[ReadModel]
public record QueryAccountBalance(decimal Balance)
{
    [AllowAnonymous]
    public static Task<QueryAccountBalance> ById(string id, IReadModels readModels) =>
        readModels.GetInstanceById<QueryAccountBalance>(new ReadModelKey(id));

    [AllowAnonymous]
    public static async Task<QueryAccountBalance> FromOther(string id, IReadModels readModels)
    {
        var other = await readModels.GetInstanceById<OtherAccountBalance>(new ReadModelKey(id));
        return new QueryAccountBalance(other.Balance);
    }
}
