// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Testing.Commands.for_CommandScenario.generated_values.given;

public record GenerationObservation(ContextId Id, DateTimeOffset ReceivedAt, DateTimeOffset Clock, TenantId Tenant);
