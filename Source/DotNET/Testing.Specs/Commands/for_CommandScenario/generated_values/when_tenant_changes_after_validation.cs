// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Testing.Commands.for_CommandScenario.generated_values;

public class when_tenant_changes_after_validation : Specification
{
    CommandScenario<given.ObserveGeneration> _scenario;
    given.GenerationObservation _observation;
    readonly DateTimeOffset _instant = new(2024, 6, 15, 12, 34, 56, TimeSpan.Zero);

    async Task Establish()
    {
        _scenario = new CommandScenario<given.ObserveGeneration>().Clock(_instant);
        await _scenario.Validate(new());
    }

    async Task Because()
    {
        _scenario.Tenant(new TenantId("tenant-two")).AllowGeneration();
        _observation = ((CommandResult<given.GenerationObservation>)await _scenario.Execute(new())).Response!;
    }

    void Destroy() => _scenario.Dispose();

    [Fact] void should_keep_validation_from_freezing_tenant_configuration() => _observation.Id.Value.ShouldEqual($"{_instant:O}|tenant-two");
}
