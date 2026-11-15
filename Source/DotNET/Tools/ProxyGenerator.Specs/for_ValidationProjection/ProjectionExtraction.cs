// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_ValidationProjection;

static class ProjectionExtraction
{
    internal static (List<PropertyValidationDescriptor> Rules, ValidationProjectionRecord[] Records) For(Type type)
    {
        var report = new ValidationProjectionReport();
        using var recording = ValidationProjectionRecorder.BeginReport(report);
        using var endpoint = ValidationProjectionRecorder.Begin(new("command", type.Name, "/test"));
        var rules = ValidationRulesExtractor.ExtractValidationRules(type.Assembly, type).ToList();
        ValidationProjectionRecorder.CompleteEndpoint();
        return (rules, report.Ordered().ToArray());
    }
}
