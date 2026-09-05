// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// The other shape Arc documents drives a running host rather than the pipeline in process: what the event log
/// already held is appended to it directly, the command goes over HTTP, and the steps live on a nested context with
/// only the assertions left outside. It says the same thing as the in-process shape and has to read as the same
/// thing, or a document would describe an application differently depending on how it was specified.
/// </summary>
public class a_specification_against_a_running_host : Specification
{
    const string Slice = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Authors.Registration;

        [EventType]
        public record AuthorRegistered(string Name);

        [Command]
        public record RegisterAuthor(string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }
        """;

    const string Scenario = """
        using System.Net.Http;
        using System.Threading.Tasks;
        using Cratis.Chronicle.EventSequences;
        using Cratis.Chronicle.Testing.EventSequences;
        using Cratis.Chronicle.XUnit.Integration;
        using Library.Authors.Registration;
        using Xunit;

        namespace Library.Authors.Registration.when_registering;

        public class and_the_name_is_already_taken
        {
            public class context
            {
                public const string AuthorName = "Jane Austen";

                protected IEventLog EventLog = null!;
                protected HttpClient Client = null!;

                async Task Establish() => await EventLog.Append("author", new AuthorRegistered(AuthorName));

                async Task Because() => await Client.ExecuteCommand("/api/authors/register", new RegisterAuthor(AuthorName));
            }

            readonly IEventSequence _sequence = null!;

            [Fact] void should_register_the_author() => _sequence.ShouldHaveAppendedEvent<AuthorRegistered>("author");
        }
        """;

    static readonly (string Path, string Text)[] _sources =
    [
        ("Library/Authors/Registration/Registration.cs", Slice),
        ("Library/Authors/Registration/when_registering/and_the_name_is_already_taken.cs", Scenario),
        (IntegrationTesting.Path, IntegrationTesting.Source)
    ];

    ApplicationModelAnalysis _analysis;
    SpecificationModel _specification;

    void Establish()
    {
        _analysis = Analyzed.Source(_sources);
        _specification = _analysis.Model.Slices.Single(_ => _.Name == "Registration").Specifications.Single();
    }

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_sources).ShouldBeEmpty();
    [Fact] void should_read_the_steps_written_on_the_nested_context() => _specification.Given.Single().Name.ShouldEqual("AuthorRegistered");
    [Fact] void should_state_the_value_appended_to_the_log() => _specification.Given.Single().Values.ShouldContainOnly([new PropertyMappingModel("Name", new LiteralSource("Jane Austen"))]);
    [Fact] void should_issue_the_command_sent_over_http() => _specification.When.Name.ShouldEqual("RegisterAuthor");
    [Fact] void should_state_the_values_the_command_was_sent_with() => _specification.When.Values.ShouldContainOnly([new PropertyMappingModel("Name", new LiteralSource("Jane Austen"))]);
    [Fact] void should_read_the_assertions_left_outside_the_context() => _specification.Then.Single().Name.ShouldEqual("AuthorRegistered");
    [Fact] void should_report_nothing() => _analysis.Diagnostics.ShouldBeEmpty();
}
