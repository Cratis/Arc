// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

internal static class DenialScenarioSources
{
    internal static (string Path, string Text)[] With(string setup, string authorization = "[Authorize]", string composition = "", string assertions = "_result.ShouldNotBeAuthorized();") =>
        CallerScenarioSources.With(setup, authorization, composition)
            .Select(source => (source.Path, Text: source.Text
                .Replace("public Task<CommandResult> Execute(TCommand command) => Task.FromResult(new CommandResult());", "public Task<Cratis.Arc.Commands.CommandResult> Execute(TCommand command) => Task.FromResult(new Cratis.Arc.Commands.CommandResult());", StringComparison.Ordinal)
                .Replace("readonly HttpClient _client = new();", "readonly HttpClient _client = new(); Cratis.Arc.Commands.CommandResult _result = null!;", StringComparison.Ordinal)
                .Replace("async Task Because() => await", "async Task Because() => _result = await", StringComparison.Ordinal)
                .Replace("[Fact] Task should_register() => _scenario.EventSequence.ShouldHaveAppendedEvent<AuthorRegistered>(\"author\", e => e.Name == \"Apollo\");", $"[Fact] void should_deny() {{ {assertions} }}", StringComparison.Ordinal)))
            .Append(("Library/Testing/DenialAssertions.cs", """
                namespace Cratis.Arc.Testing.Commands
                {
                    public static class CommandResultShouldExtensions
                    {
                        public static void ShouldNotBeAuthorized(this Cratis.Arc.Commands.CommandResult result) { }
                        public static void ShouldNotBeSuccessful(this Cratis.Arc.Commands.CommandResult result) { }
                        public static void ShouldHaveValidationErrors(this Cratis.Arc.Commands.CommandResult result) { }
                    }
                }
                """))
            .ToArray();
}
