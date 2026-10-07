// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_converted_response_record : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, """
        using Cratis.Arc.Commands.ModelBound;
        namespace Library.Authors.Registration;
        public record Receipt(string Name)
        {
            public static implicit operator string(Receipt receipt) => receipt.Name.ToUpperInvariant();
        }
        [Command] public record EchoName(string Name)
        {
            public string Handle() => new Receipt(Name);
        }
        """));

    [Fact] void should_keep_the_handler() => Result.Source.ShouldContain("handler");
    [Fact] void should_not_claim_the_unconverted_record_is_the_response() => Result.Source.ShouldNotContain("returns");
    [Fact] void should_report_the_response_at_the_command() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse).Location.ShouldEqual("Library.Authors.Registration.EchoName");
    [Fact] void should_still_compile_and_round_trip()
    {
        new ScreenplayCompiler().Compile(Result.Source).Success.ShouldBeTrue();
        RoundTrip.IsStable.ShouldBeTrue();
    }
}
