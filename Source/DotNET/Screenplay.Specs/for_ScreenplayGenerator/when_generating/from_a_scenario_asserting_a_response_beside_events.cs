// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_scenario_asserting_a_response_beside_events : a_generated_document
{
    const string Name = "WhenRegisteringAndANameIsSupplied";

    const string ScalarHandler = "public (AuthorRegistered, string) Handle() => (new(Name), Name);";

    const string RecordHandler = "public (AuthorRegistered, Registration) Handle() => (new(Name), new Registration(Id, Name));";

    [Fact]
    public void should_state_a_scalar_response_beside_the_event()
    {
        GenerateScenario(ScalarHandler, "((CommandResult<string>)_result).Response.ShouldEqual(\"Apollo\")");
        Result.Source.ShouldContain("returns name");
        Result.Source.ShouldContain("then returns \"Apollo\"");
        Result.Source.ShouldContain("then AuthorRegistered");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse).ShouldBeFalse();
        AssertDocument();
        Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V7);
    }

    [Fact]
    public void should_keep_the_response_for_a_scenario_not_asserting_it()
    {
        GenerateScenario(ScalarHandler, null);
        Result.Source.ShouldContain("returns name");
        Result.Source.ShouldContain($"specification {Name}");
        Result.Source.ShouldNotContain("then returns");
        AssertDocument();
    }

    [Theory]
    [InlineData("((CommandResult<Registration>)_result).Response!.Name.ShouldEqual(\"Apollo\")", "name = \"Apollo\"")]
    [InlineData("Assert.Equal(\"current\", ((CommandResult<Registration>)_result).Response!.Id)", "id = \"current\"")]
    [InlineData("((CommandResult<Registration>)_result).Response.ShouldEqual(new Registration(\"current\", \"Apollo\"))", "id = \"current\"\n    name = \"Apollo\"")]
    public void should_state_a_record_response_subset(string assertion, string fields)
    {
        GenerateScenario(RecordHandler, assertion);
        Result.Source.ShouldContain("returns\n");
        string.Join('\n', Result.Source.Split('\n').Select(line => line.TrimStart()))
            .ShouldContain("then returns\n" + string.Join('\n', fields.Split('\n').Select(line => line.TrimStart())));
        AssertDocument();
    }

    [Theory]
    [InlineData(ScalarHandler, "((CommandResult<string>)_result).Response.ShouldNotBeNull()")]
    [InlineData(ScalarHandler, "((CommandResult<string>)_result).Response.ShouldEqual(_name)")]
    [InlineData(RecordHandler, "((CommandResult<Registration>)_result).Response!.Name.ShouldNotBeNull()")]
    [InlineData(RecordHandler, "((CommandResult<Registration>)_result).Response!.Name.Length.ShouldEqual(6)")]
    public void should_keep_the_legacy_command_and_scenario_when_the_response_assertion_cannot_be_stated(string handler, string assertion)
    {
        GenerateScenario(handler, assertion);
        Result.Source.ShouldNotContain("returns");
        Result.Source.ShouldContain($"specification {Name}");
        Result.Source.ShouldContain("then AuthorRegistered");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse &&
            diagnostic.Message.Contains("cannot be stated as then returns", StringComparison.Ordinal)).ShouldBeTrue();
        AssertDocument();
    }

    void GenerateScenario(string handler, string? assertion)
    {
        var testing = IntegrationTesting.Source.Replace(
            "public Task<CommandResult> Execute(TCommand command) => Task.FromResult(new CommandResult());",
            "public Task<Cratis.Arc.Commands.CommandResult> Execute(TCommand command) => Task.FromResult(new Cratis.Arc.Commands.CommandResult());",
            StringComparison.Ordinal);
        var slice = $$"""
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Chronicle.Events;
            using Cratis.Chronicle.Keys;

            namespace Library.Authors.Registration;

            [EventType] public record AuthorRegistered(string Name);
            public record Registration(string Id, string Name);
            [Command] public record RegisterAuthor([Key] string Id, string Name)
            {
                {{handler}}
            }
            """;
        var scenario = $$"""
            using System.Threading.Tasks;
            using Cratis.Arc.Commands;
            using Cratis.Arc.Testing.Commands;
            using Cratis.Arc.Chronicle.Testing.Commands;
            using Cratis.Chronicle.Testing.EventSequences;
            using Cratis.Specifications;
            using Library.Authors.Registration;
            using Xunit;
            namespace Library.Authors.Registration.when_registering;
            public class and_a_name_is_supplied
            {
                readonly string _name = "Apollo";
                readonly CommandScenario<RegisterAuthor> _scenario = new();
                Cratis.Arc.Commands.CommandResult _result = null!;
                async Task Because() => _result = await _scenario.Execute(new RegisterAuthor("current", "Apollo"));
                [Fact] Task should_register_the_author() => _scenario.EventSequence.ShouldHaveAppendedEvent<AuthorRegistered>("current", e => e.Name == "Apollo");
                {{(assertion is null ? string.Empty : $"[Fact] void should_return_the_response() => {assertion};")}}
            }
            """;
        (string Path, string Text)[] sources =
        [
            (Analyzed.SlicePath, slice),
            (IntegrationTesting.Path, testing),
            ("Library/Authors/Registration/when_registering/and_a_name_is_supplied.cs", scenario)
        ];
        Analyzed.ErrorsIn(sources).ShouldBeEmpty();
        Generate(sources);
    }
}
