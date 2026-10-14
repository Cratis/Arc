// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_literal_inline_data : a_generated_document
{
    void Because() => Generate(LiteralCaseSources.With("""
        using System.Threading.Tasks;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Arc.Chronicle.Testing.Commands;
        using Cratis.Chronicle.Testing.EventSequences;
        using Xunit;
        namespace Library.Authors.Registration;
        public class registering
        {
            readonly CommandScenario<RegisterAuthor> _scenario = new();
            [Theory] [InlineData("Jane")] [InlineData("John")]
            public async Task should_append(string name)
            {
                await _scenario.Execute(new RegisterAuthor("author", name));
                await _scenario.EventSequence.ShouldHaveAppendedEvent<AuthorRegistered>("author", e => e.Name == name);
            }
        }
        """));

    [Fact] void should_report_the_separate_reader_needed() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.SpecificationCasesNotFolded).Message.ShouldContain("separate theory-method reader");
    [Fact] void should_not_pretend_the_method_is_a_table() => Result.Source.ShouldNotContain("specification Registering");
    [Fact] void should_round_trip_and_bind() => AssertDocument();
}
