// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

internal static class SliceRejectionSources
{
    internal const string Members = "readonly Slice<RegisterAuthor> _slice = new(new RegisterAuthor(\"\")); SliceOutcome<RegisterAuthor> _outcome = null!;";

    const string ScenarioWithServices = """
        public class CommandScenario<TCommand>
            {
                public Microsoft.Extensions.DependencyInjection.IServiceCollection Services { get; } = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
                public System.Collections.Generic.IDictionary<string, object> Context { get; } = new System.Collections.Generic.Dictionary<string, object>();
        """;

    internal static (string Path, string Text)[] WithInheritedSetup(string additionalSetup = "")
    {
        var inheritedSetup = $$"""
            public abstract class a_slice
            {
                protected Slice<RegisterAuthor> _slice = null!;
                protected SliceOutcome<RegisterAuthor> _outcome = null!;
                void Establish() { _slice = new(new RegisterAuthor("")); {{additionalSetup}} }
            }
            public class with_an_empty_name : a_slice
            """;
        return With(members: "").Select(source => (source.Path, source.Text.Replace(
            "public class with_an_empty_name",
            inheritedSetup,
            StringComparison.Ordinal))).ToArray();
    }

    internal static (string Path, string Text)[] With(
        string action = "_outcome = await _slice.Run()",
        string assertion = "_outcome.ShouldBeRejected()",
        string setup = "",
        string members = Members) =>
    [
        ("Library/Authors/Registration/RegisterAuthor.cs", """
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Chronicle.Events;
            namespace Library.Authors.Registration;
            [EventType] public record AuthorRegistered(string Name);
            [Command] public record RegisterAuthor(string Name)
            {
                public AuthorRegistered Handle() => new(Name);
            }
            """),
        (IntegrationTesting.Path, IntegrationTesting.Source.Replace(
            "public class CommandScenario<TCommand>\n    {",
            ScenarioWithServices,
            StringComparison.Ordinal)),
        ("Library/Testing/Slices.cs", IntegrationTesting.Slices),
        ("Library/Authors/Registration/when_registering/with_an_empty_name.cs", $$"""
            using System.Security.Claims;
            using System.Threading.Tasks;
            using Cratis.Arc.Chronicle.Testing.Slices;
            using Library.Authors.Registration;
            using Xunit;
            namespace Library.Authors.Registration.when_registering;
            public class with_an_empty_name
            {
                {{members}}
                void Establish() { {{setup}} }
                async Task Because() { {{action}}; }
                [Fact] void should_be_rejected() { {{assertion}}; }
            }
            """)
    ];
}
