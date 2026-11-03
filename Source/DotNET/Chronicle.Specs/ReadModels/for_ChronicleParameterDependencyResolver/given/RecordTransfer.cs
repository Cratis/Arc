// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.ReadModels;
using static Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver.given.protected_plain_models;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver.given;

[Command]
[ReadProtection(ReadProtectionMode.On)]
public record RecordTransfer(AccountId From, AccountId To)
{
    public TransferRecorded Handle(
        [LookUpBy(nameof(From))] Balance from,
        [LookUpBy(nameof(To))] Balance to) => new(from.Id, to.Id);
}
