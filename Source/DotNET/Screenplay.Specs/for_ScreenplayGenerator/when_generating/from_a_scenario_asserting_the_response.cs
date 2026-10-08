// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_scenario_asserting_the_response : a_generated_document
{
    const string Command = """
        using Cratis.Arc.Commands.ModelBound;
        namespace Library.Authors.Registration;
        [Command] public record EchoName(string Name)
        {
            public string Handle() => Name;
        }
        """;

    const string Testing = """
        using System.Threading.Tasks;
        using Cratis.Arc.Commands;
        namespace Cratis.Arc.Testing.Commands;
        public class CommandScenario<TCommand>
        {
            public Task<CommandResult> Execute(TCommand command) => Task.FromResult(new CommandResult());
        }
        """;

    const string EqualAssertion = "Assert.Equal(\"Apollo\", ((CommandResult<string>)_result).Response)";

    void Because() => GenerateScenario(EqualAssertion);

    [Fact] void should_keep_the_command_response() => Result.Source.ShouldContain("returns name");
    [Fact] void should_state_the_scenario() => Result.Source.ShouldContain("specification WhenEchoingAndANameIsSupplied");
    [Fact] void should_state_the_response_expectation() => Result.Source.ShouldContain("then returns \"Apollo\"");
    [Fact] void should_not_withhold_the_response() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse).ShouldBeFalse();
    [Fact] void should_bind_and_round_trip() => AssertDocument();
    [Fact] void should_select_v7() => Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V7);
    [Fact] void should_pass_reference_execution() => Assert.True(Run("WhenEchoingAndANameIsSupplied").Passed, string.Join(Environment.NewLine, Run("WhenEchoingAndANameIsSupplied").Failures));

    [Theory]
    [InlineData("((CommandResult<string>)_result).Response.ShouldEqual(\"Apollo\")")]
    [InlineData("((CommandResult<string>)_result).Response!.ShouldEqual(Expected)")]
    [InlineData("Assert.Equal(expected: \"Apollo\", actual: ((CommandResult<string>)_result).Response)")]
    public void should_state_other_equality_assertions(string assertion)
    {
        GenerateScenario(assertion);
        Result.Source.ShouldContain("then returns \"Apollo\"");
        AssertDocument();
    }

    [Theory]
    [InlineData("((CommandResult<string>)_result).Response.ShouldNotBeNull()")]
    [InlineData("((CommandResult<string>)_result).Response.ShouldEqual(\"Apo\" + _suffix)")]
    [InlineData("Assert.Equal(_result.ToString(), ((CommandResult<string>)_result).Response)")]
    [InlineData("{ if (_result is not null) ((CommandResult<string>)_result).Response.ShouldEqual(\"Apollo\"); }")]
    public void should_leave_out_a_response_only_scenario_it_cannot_state(string assertion)
    {
        GenerateScenario(assertion);
        Result.Source.ShouldNotContain("specification");
        Result.Source.ShouldContain("returns name");
        Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).Message.ShouldContain("CommandResult.Response");
        AssertDocument();
    }

    [Fact]
    public void should_not_state_a_response_beyond_the_executable_model_version_cap()
    {
        GenerateScenario(new ScreenplayOptions { MaximumExecutableModelVersion = SemanticVersion.V6 }, EqualAssertion);
        Result.Source.ShouldNotContain("returns");
        Result.Source.ShouldNotContain("specification");
        Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).Message.ShouldContain("does not return");
        RoundTrip.Errors.ShouldBeEmpty();
    }

    void GenerateScenario(string assertion) => GenerateScenario(new ScreenplayOptions(), assertion);

    void GenerateScenario(ScreenplayOptions options, string assertion)
    {
        var body = assertion.StartsWith('{') ? assertion : $"=> {assertion};";
        var scenario = $$"""
            using System.Threading.Tasks;
            using Cratis.Arc.Commands;
            using Cratis.Arc.Testing.Commands;
            using Cratis.Specifications;
            using Library.Authors.Registration;
            using Xunit;
            namespace Library.Authors.Registration.when_echoing;
            public class and_a_name_is_supplied
            {
                const string Expected = "Apollo";
                readonly string _suffix = "llo";
                readonly CommandScenario<EchoName> _scenario = new();
                CommandResult _result = null!;
                async Task Because() => _result = await _scenario.Execute(new EchoName("Apollo"));
                [Fact] void should_return_the_name() {{body}}
            }
            """;
        (string Path, string Text)[] sources =
        [
            (Analyzed.SlicePath, Command),
            ("Library/Testing/Commands.cs", Testing),
            ("Library/Authors/Registration/when_echoing/and_a_name_is_supplied.cs", scenario)
        ];
        Analyzed.ErrorsIn(sources).ShouldBeEmpty();
        Generate(options, sources);
    }
}
