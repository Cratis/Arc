// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.MissingWithChronicleAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_MissingWithChronicleAnalyzer.when_setting_up_arc;

public class and_there_are_no_chronicle_artifacts : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
namespace TestNamespace
{
    public static class Setup
    {
        public static object AddCratisArc(this object builder) => builder;
    }

    public record RegisterAuthor(string Name);

    public static class Program
    {
        public static void Configure(object builder) => builder.AddCratisArc();
    }
}"));

    [Fact] void should_not_report_diagnostic() => _result.ShouldBeNull();
}
