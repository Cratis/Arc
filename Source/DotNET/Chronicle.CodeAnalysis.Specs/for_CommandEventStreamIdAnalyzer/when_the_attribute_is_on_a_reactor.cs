// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandEventStreamIdAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_CommandEventStreamIdAnalyzer;

public class when_the_attribute_is_on_a_reactor
{
    const string Preamble = @"
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Reactors;
        ";

    [Theory]
    [InlineData("{Name}")]
    [InlineData("scope:{Period}")]
    public async Task should_warn_about_a_template(string template) =>
        await VerifyCS.VerifyAnalyzerAsync(
            Preamble + "[EventStreamId({|#0:\"" + template + "\"|})] public class R : IReactor;",
            new ExpectedDiagnostic("ARCCHR0019", DiagnosticSeverity.Warning, "R", template));

    [Theory]
    [InlineData("constant")]
    [InlineData("{{escaped}}")]
    [InlineData("")]
    public async Task should_not_warn_about_a_constant(string value) =>
        await VerifyCS.VerifyAnalyzerAsync(Preamble + "[EventStreamId(\"" + value + "\")] public class R : IReactor;");

    [Fact]
    public async Task should_not_warn_about_a_type_that_is_not_a_reactor() =>
        await VerifyCS.VerifyAnalyzerAsync(Preamble + "[EventStreamId(\"{Name}\")] public class R;");
}
