// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_resolving_template_stream_ids.given;

public class a_template_report_scenario : Specification
{
    protected CommandScenario<GenerateTemplateReport> _scenario;
    protected GenerateTemplateReport _command;

    void Establish()
    {
        _scenario = new();
        _command = new(EventSourceId.New(), "reporting", new(2026, 10, 1));
    }

    async Task Destroy() => await _scenario.DisposeAsync();
}
