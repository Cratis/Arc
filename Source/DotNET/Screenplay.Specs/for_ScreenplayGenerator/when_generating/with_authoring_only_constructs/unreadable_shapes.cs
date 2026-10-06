// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_authoring_only_constructs;

public class unreadable_shapes : an_authoring_document
{
    void Because() => Generate("""
        using System;
        using Cratis.Arc.Commands;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        namespace Library.Authors.Registration;
        [EventType] public record AuthorRegistered(string Name);
        [ReadModel] public record AuthorState(string Name);
        public record CollectionResponse(string[] Names);
        [Command] public record WithACollectionResponse(string[] Names)
        {
            public (CollectionResponse, AuthorRegistered) Handle() => (new(Names), new("fixed"));
        }
        public interface IMailer;
        public interface IAccountingClient;
        public record Notify(string Name) : ICommandOperation
        {
            public void Execute(IMailer mailer, IAccountingClient accounting) { }
        }
        [Command] public record WithAComputedResponse(string Name)
        {
            public (string, AuthorRegistered) Handle()
            {
                var slug = Name.ToLowerInvariant();
                return (slug, new(Name));
            }
        }
        [Command] public record WithSeveralSystems(string Name)
        {
            public (AuthorRegistered, Notify) Handle() => (new(Name), new Notify(Name));
        }
        [Command, EventSourceType("Account"), EventStreamType("Transactions")]
        public record WithAComputedRoute(string Name, int Month) : ICanProvideEventStreamId
        {
            public EventStreamId GetEventStreamId() => Month.ToString();
            public AuthorRegistered Handle() => new(Name);
        }
        [Command] public record WithImperativeProvisioning(string Name)
        {
            public AuthorState Provide() => new(Name.ToLowerInvariant());
            public AuthorRegistered Handle(AuthorState state) => new(state.Name);
        }
        """);

    [Fact] void should_report_the_non_uuid_computation() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandResponse).ShouldBeTrue();
    [Fact] void should_not_generate_a_string_slug() => Result.Source.Contains("generated", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_report_multiple_operation_systems() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandOperation).ShouldBeTrue();
    [Fact] void should_not_emit_an_operation_the_grammar_cannot_hold() => Result.Source.Contains("produces operation Notify", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_report_the_computed_stream_id() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandRoute).ShouldBeTrue();
    [Fact] void should_report_imperative_provisioning() => Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandProvisioning).ShouldBeTrue();
    [Fact] void should_not_treat_an_arbitrary_provided_value_as_a_di_read() => Result.Source.Contains("reads AuthorState", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_emit_a_collection_response() => Result.Model.Slices.SelectMany(slice => slice.Commands).Single(command => command.Name == "WithACollectionResponse").Authoring!.ResponseFields.ShouldBeEmpty();
    [Fact] void should_compile_the_remaining_document() => Compiled.Success.ShouldBeTrue();
}
