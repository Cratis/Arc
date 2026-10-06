// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_scenario_asserting_the_response : a_generated_document
{
    void Because()
    {
        (string Path, string Text)[] sources =
        [
        (Analyzed.SlicePath, """
        using Cratis.Arc.Commands.ModelBound;
        namespace Library.Authors.Registration;
        [Command] public record EchoName(string Name)
        {
            public string Handle() => Name;
        }
        """),
        ("Library/Testing/Commands.cs", """
        using System.Threading.Tasks;
        using Cratis.Arc.Commands;
        namespace Cratis.Arc.Testing.Commands;
        public class CommandScenario<TCommand>
        {
            public Task<CommandResult> Execute(TCommand command) => Task.FromResult(new CommandResult());
        }
        """),
        ("Library/Authors/Registration/when_echoing/and_a_name_is_supplied.cs", """
        using System.Threading.Tasks;
        using Cratis.Arc.Commands;
        using Cratis.Arc.Testing.Commands;
        using Library.Authors.Registration;
        using Xunit;
        namespace Library.Authors.Registration.when_echoing;
        public class and_a_name_is_supplied
        {
            readonly CommandScenario<EchoName> _scenario = new();
            CommandResult _result = null!;
            async Task Because() => _result = await _scenario.Execute(new EchoName("Apollo"));
            [Fact] void should_return_the_name() => Assert.Equal("Apollo", ((CommandResult<string>)_result).Response);
        }
        """)
        ];
        Generate(sources);
    }

    [Fact] void should_not_emit_a_partial_scenario() => Result.Source.ShouldNotContain("specification");
    [Fact] void should_explain_the_missing_response_expectation() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).Message.ShouldContain("CommandResult.Response");
    [Fact] void should_keep_the_command_response() => Result.Source.ShouldContain("returns name");
    [Fact] void should_bind_and_round_trip() => AssertDocument();
}
