// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_resolving_template_stream_ids;

public class GenerateTemplateReportValidator : CommandValidator<GenerateTemplateReport>
{
    public GenerateTemplateReportValidator()
    {
        RuleFor(command => command.ReportingScopeId).NotEmpty();
    }
}
