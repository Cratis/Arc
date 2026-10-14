// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_CommandAttributeAnalyzer;

public class when_a_lookup_property_has_a_different_key_type
{
    [Fact] public Task should_name_the_parameter_and_key_types() => AnalyzerVerifier<CommandAttributeAnalyzer>.VerifyAnalyzerAsync(
        "using Cratis.Arc.Commands; public class Command { public int Key { get; } public void Handle([LookUpBy(nameof(Key))] Model model) { } } public class Model { public string Id { get; } = string.Empty; }",
        new ExpectedDiagnostic("ARCCHR0031", DiagnosticSeverity.Error, "parameter 'model'", "property 'Key' of type 'Int32'", "key type 'String'"));
}
