// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public partial class from_a_command_scenario_with_another_sources_given
{
    const string HostScenario = """
        using System.Net.Http;
        using System.Threading.Tasks;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.EventSequences;
        using Cratis.Chronicle.Testing.EventSequences;
        using Cratis.Chronicle.XUnit.Integration;
        using Xunit;

        namespace Library.Authors.Registration.when_registering;

        public class and_another_author_has_claimed_the_name
        {
            readonly IEventLog EventLog = null!;
            readonly HttpClient Client = null!;
            Result _result = null!;
            readonly EventSourceId _otherId = EventSourceId.New();

            async Task Establish() => await EventLog.Append("other", new AuthorRegistered("Claimed"));
            async Task Because() => _result = await Client.ExecuteCommand("/api/authors/register", new RegisterAuthor("current", "Claimed"));
            [Fact] void should_reject_the_duplicate() => _result.ShouldHaveConstraintViolationFor("unique-author-name");
        }
        """;

    [Theory]
    [InlineData("Append(\"other\", new AuthorRegistered(\"Claimed\"))")]
    [InlineData("AppendMany(\"other\", new[] { new AuthorRegistered(\"Claimed\") })")]
    public void should_state_another_source_seeded_through_the_host(string append)
    {
        GenerateScenario(Slice, HostScenario.Replace("Append(\"other\", new AuthorRegistered(\"Claimed\"))", append, StringComparison.Ordinal));
        Result.Source.ShouldContain("for \"other\"");
        AssertDocument();
    }

    [Theory]
    [InlineData("Append(\"other\", new AuthorRegistered(\"Claimed\"))")]
    [InlineData("AppendMany(\"other\", new[] { new AuthorRegistered(\"Claimed\") })")]
    public void should_keep_the_hosts_command_source_implicit(string append)
    {
        GenerateScenario(Slice, HostScenario
            .Replace("Append(\"other\", new AuthorRegistered(\"Claimed\"))", append, StringComparison.Ordinal)
            .Replace("RegisterAuthor(\"current\",", "RegisterAuthor(\"other\",", StringComparison.Ordinal));
        AssertImplicitSource();
    }

    [Fact] void should_omit_an_undecidable_source_seeded_through_the_host()
    {
        GenerateScenario(Slice, HostScenario.Replace("Append(\"other\",", "Append(_otherId,", StringComparison.Ordinal));
        AssertOmitted();
    }
}
