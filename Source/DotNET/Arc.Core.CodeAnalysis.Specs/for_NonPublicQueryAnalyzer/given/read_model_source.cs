// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.CodeAnalysis.for_NonPublicQueryAnalyzer.given;

#pragma warning disable MA0136 // Compiler fixture behavior is independent of line endings.

public class read_model_source : Specification
{
    protected static string Source(string methods, bool explicitMode = false) => $$"""
        #nullable enable
        using System.Collections.Generic;
        using System.Reactive.Subjects;
        using System.Threading.Tasks;
        using Cratis.Arc.Queries.ModelBound;
        namespace Example;
        [ReadModel(RequireQueryAttribute = {{(explicitMode ? "true" : "false")}})]
        internal class Order
        {
            {{methods}}
        }
        """;

    protected static ExpectedDiagnostic Warning(string name) => AnalyzerVerifier<NonPublicQueryAnalyzer>.Diagnostic("ARC0029")
        .WithSeverity(DiagnosticSeverity.Warning).WithArguments(name, "Order");
}
