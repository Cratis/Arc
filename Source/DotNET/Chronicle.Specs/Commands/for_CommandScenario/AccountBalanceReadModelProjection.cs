// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario;

public class AccountBalanceReadModelProjection : IProjectionFor<AccountBalanceReadModel>
{
    public void Define(IProjectionBuilderFor<AccountBalanceReadModel> builder)
    {
    }
}
