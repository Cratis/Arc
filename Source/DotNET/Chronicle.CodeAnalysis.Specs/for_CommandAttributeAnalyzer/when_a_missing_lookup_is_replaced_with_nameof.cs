// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.CodeAnalysis.CodeFixes;
using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_CommandAttributeAnalyzer;

public class when_a_missing_lookup_is_replaced_with_nameof
{
    [Fact] public Task should_correct_the_property_and_use_nameof() => CodeFixVerifier<CommandAttributeAnalyzer, UseNameofForLookUpByCodeFixProvider>.VerifyCodeFixAsync(
        "using Cratis.Arc.Commands; public class Command { public string Key { get; } = string.Empty; public void Handle([LookUpBy(\"Kye\")] Model model) { } } public class Model;",
        "using Cratis.Arc.Commands; public class Command { public string Key { get; } = string.Empty; public void Handle([LookUpBy(nameof(global::Command.Key))] Model model) { } } public class Model;",
        new ExpectedDiagnostic("ARCCHR0031", DiagnosticSeverity.Error, "parameter 'model'", "missing public instance property 'Kye'", "did you mean 'Key'"));
}
