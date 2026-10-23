// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Queries.for_QueryScenario;

[Command]
[AllowAnonymous]
[ReadProtection(ReadProtectionMode.Measure)]
public record ChangeObservedAccountWithReadProtection(EventSourceId Id, decimal Balance)
{
    public ObservedAccountChanged Handle() => new(Balance);
}
