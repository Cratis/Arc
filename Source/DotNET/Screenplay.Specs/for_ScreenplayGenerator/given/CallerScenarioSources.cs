// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

internal static class CallerScenarioSources
{
    internal const string ScenarioName = "WhenRegisteringWithACaller";
    internal const string RolePrincipal = "new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, \"Librarian\")], authenticationType: \"test\"))";

    internal static (string Path, string Text)[] With(string setup, string authorization = "[Authorize]", string composition = "", string action = "_scenario.Execute(new RegisterAuthor(\"author\", \"Apollo\"))") =>
    [
        ("Library/PolicyFramework.cs", for_ApplicationModelAnalyzer.when_analyzing.PolicySource.Framework),
        (Analyzed.SlicePath, $$"""
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Chronicle.Events;
            using Cratis.Chronicle.Keys;
            using Cratis.Arc.Authorization;
            namespace Library.Authors.Registration;
            [EventType] public record AuthorRegistered(string Name);
            [Command] {{authorization}} public record RegisterAuthor([Key] string Id, string Name)
            {
                public AuthorRegistered Handle() => new(Name);
            }
            {{composition}}
            """),
        (IntegrationTesting.Path, IntegrationTesting.Source.Replace(
            "public class CommandScenario<TCommand>\n    {",
            "public class CommandScenario<TCommand>\n    {\n        public Microsoft.Extensions.DependencyInjection.IServiceCollection Services { get; } = new Microsoft.Extensions.DependencyInjection.ServiceCollection();",
            StringComparison.Ordinal)),
        ("Library/Authors/Registration/when_registering/with_a_caller.cs", $$"""
            using System;
            using System.Collections.Generic;
            using System.Net.Http;
            using System.Security.Claims;
            using System.Threading.Tasks;
            using Cratis.Arc.Authorization;
            using Cratis.Arc.Testing.Commands;
            using Cratis.Arc.Chronicle.Testing.Commands;
            using Cratis.Chronicle.Testing.EventSequences;
            using Cratis.Chronicle.XUnit.Integration;
            using Microsoft.Extensions.DependencyInjection;
            using NSubstitute;
            using Xunit;
            namespace Library.Authors.Registration.when_registering;
            public class with_a_caller
            {
                readonly CommandScenario<RegisterAuthor> _scenario = new();
                readonly CommandScenario<RegisterAuthor> _other = new();
                readonly HttpClient _client = new();
                static string Computed() => "tenant";
                void Establish() { {{setup}} }
                async Task Because() => await {{action}};
                [Fact] Task should_register() => _scenario.EventSequence.ShouldHaveAppendedEvent<AuthorRegistered>("author", e => e.Name == "Apollo");
            }
            """)
    ];

    internal static string Setup(string principal) => $$"""
        var accessor = Substitute.For<ICurrentPrincipalAccessor>();
        accessor.Current.Returns({{principal}});
        _scenario.Services.AddSingleton(accessor);
        """;
}
