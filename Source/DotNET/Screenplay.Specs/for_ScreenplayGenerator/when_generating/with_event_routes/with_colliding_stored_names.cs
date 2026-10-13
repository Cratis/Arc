// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_event_routes;

public class with_colliding_stored_names : a_generated_document
{
    [Theory]
    [InlineData(false, "crm-account", "bank-transactions")]
    [InlineData(false, "bank-account", "crm-transactions")]
    [InlineData(true, "Account", "Transactions")]
    void should_withhold_routes_to_incompatible_stored_destinations(bool legacy, string storedSource, string storedStream)
    {
        var definitionName = legacy ? "LedgerEventSource" : "AccountEventSource";
        var stream = legacy ? "\"Transactions\"" : "Transactions";
        const string Bank = """
                namespace Library.EventSources.Bank;
                [Cratis.Chronicle.EventSources.EventSource("bank-account")]
                [Cratis.Chronicle.EventSources.EventStream(AccountEventSource.Transactions)]
                public class AccountEventSource : Cratis.Chronicle.EventSources.IEventSource
                {
                    public const string Transactions = "bank-transactions";
                }
                """;
        var crm = $$"""
                namespace Library.EventSources.Crm;
                [Cratis.Chronicle.EventSources.EventSource("{{storedSource}}")]
                [Cratis.Chronicle.EventSources.EventStream({{stream}})]
                public class {{definitionName}} : Cratis.Chronicle.EventSources.IEventSource
                {
                    public const string Transactions = "{{storedStream}}";
                }
                """;
        var commands = IdentifierSources.With($$"""
                [Command, Cratis.Arc.Chronicle.Commands.EventSource<Library.EventSources.Bank.AccountEventSource>(Library.EventSources.Bank.AccountEventSource.Transactions)]
                public record RegisterAuthor(AuthorId AuthorId, string Name)
                {
                    public AuthorRegistered Handle() => new(Name);
                }
                [Command, Cratis.Arc.Chronicle.Commands.EventSource<Library.EventSources.Crm.{{definitionName}}>(Library.EventSources.Crm.{{definitionName}}.Transactions)]
                public record UpdateAuthor(AuthorId AuthorId, string Name)
                {
                    public AuthorRegistered Handle() => new(Name);
                }
                """);
        Generate(("Library/EventSources/Bank.cs", Bank), ("Library/EventSources/Crm.cs", crm), (Analyzed.SlicePath, commands));

        Result.Source.ShouldNotContain("stream Account.Transactions");
        Result.Source.ShouldNotContain("eventsource Account");
        Result.Diagnostics.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandRoute).ShouldEqual(2);
        AssertDocument();
    }
}
