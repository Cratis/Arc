// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_resolving_template_stream_ids;

[Command]
[EventStreamType("template-reports")]
[EventStreamId("{ReportingScopeId}:{Period}")]
public record GenerateTemplateReport(EventSourceId Id, string? ReportingScopeId, DateOnly Period)
{
    public TemplateReportGenerated Handle() => new(Period);
}
