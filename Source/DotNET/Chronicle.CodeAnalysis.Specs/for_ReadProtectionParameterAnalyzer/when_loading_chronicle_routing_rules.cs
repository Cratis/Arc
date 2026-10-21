// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_ReadProtectionParameterAnalyzer;

public class when_loading_chronicle_routing_rules
{
    [Theory]
    [InlineData("CHR0062", DiagnosticSeverity.Info)]
    [InlineData("CHR0063", DiagnosticSeverity.Warning)]
    public void should_supply_the_chronicle_owned_rule_from_the_released_package(string id, DiagnosticSeverity severity)
    {
        var descriptors = Assembly.Load("Cratis.Chronicle.CodeAnalysis").GetTypes()
            .Where(type => !type.IsAbstract && typeof(DiagnosticAnalyzer).IsAssignableFrom(type))
            .SelectMany(type => ((DiagnosticAnalyzer)Activator.CreateInstance(type)!).SupportedDiagnostics);
        var descriptor = descriptors.First(rule => rule.Id == id);
        descriptor.DefaultSeverity.ShouldEqual(severity);
    }
}
