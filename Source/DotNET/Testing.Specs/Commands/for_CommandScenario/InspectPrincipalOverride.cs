// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Arc.Testing.for_CommandScenario;

[Command]
public record InspectPrincipalOverride(ICurrentPrincipalOverride Expected)
{
    public bool Handle(ICurrentPrincipalOverride actual) => ReferenceEquals(actual, Expected);
}
