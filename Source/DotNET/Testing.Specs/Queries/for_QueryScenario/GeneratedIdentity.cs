// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Arc.Testing.for_QueryScenario;

[ReadModel]
public record GeneratedIdentity(Guid Value)
{
    [AllowAnonymous]
    public static GeneratedIdentity Generate(IIdentitySource identities) => new(identities.NewGuid());
}
