// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using static Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver.given.protected_plain_models;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver.given;

[EventType("1e1ae8e5-2e30-4baa-a55f-8ecc54189fd9")]
public record TransferRecorded(AccountId From, AccountId To);
