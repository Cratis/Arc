// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_recovering_rendered_specifications.given;

/// <summary>
/// Stage's EnumMemberNumbersCorpus.V12 rendered command and scenario (corpus 4.249.0).
/// </summary>
public static class RenderedEnumCommandSources
{
    public const string Identity = "3fa85f64-5717-4562-b3fc-2c963f66afa6";

    public const string Slice = """
        using EnumMemberNumbersCorpus_V12.Common;

        namespace EnumMemberNumbersCorpus_V12.Invoices.Lifecycle.Record;

        [Command]
        [AllowAnonymous]
        public record RecordStatus(InvoiceId Id, InvoiceStatus Status) : ICanProvideEventSourceId
        {
            /// <inheritdoc/>
            public EventSourceId GetEventSourceId() => Id;

            public StatusRecorded Handle() => new(Status);
        }

        [Command]
        [AllowAnonymous]
        public record Issue(InvoiceId Id) : ICanProvideEventSourceId
        {
            /// <inheritdoc/>
            public EventSourceId GetEventSourceId() => Id;

            public StatusRecorded Handle() => new(InvoiceStatus.Issued);
        }

        /// <summary>
        /// The event that occurs when status recorded.
        /// </summary>
        [EventType]
        public record StatusRecorded(InvoiceStatus Status);
        """;

    public const string Common = """
        namespace EnumMemberNumbersCorpus_V12.Common;

        /// <summary>
        /// Represents invoice id.
        /// </summary>
        public record InvoiceId(Guid Value) : EventSourceId<Guid>(Value), IGeneratable<InvoiceId>
        {
            public static readonly InvoiceId NotSet = new(Guid.Empty);
            public static InvoiceId New() => new(GenerateValue.Uuid());
            public static implicit operator InvoiceId(Guid value) => new(value);
        }

        /// <summary>
        /// Represents the possible values of InvoiceStatus.
        /// </summary>
        public enum InvoiceStatus
        {
            Draft = -1,
            Issued = 4,
            Cancelled = 90,
        }
        """;

    public const string Scenario = """
        using Cratis.Arc.Chronicle.Testing.Commands;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Specifications;
        using EnumMemberNumbersCorpus_V12.Common;
        using Xunit;

        namespace EnumMemberNumbersCorpus_V12.Invoices.Lifecycle.Record.when_draft_status;

        public class when_draft_status : Specification, IDisposable
        {
            readonly CommandScenario<RecordStatus> _scenario = new();
            static readonly InvoiceId _id = new InvoiceId(Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"));
            static readonly InvoiceStatus _status = InvoiceStatus.Draft;
            CommandResult _result = null!;

            async Task Because() => _result = await _scenario.Execute(new RecordStatus(_id, _status));

            [Fact] void should_succeed() => _result.ShouldBeSuccessful();
            [Fact] async Task should_have_appended_status_recorded() => await _scenario.ShouldHaveAppendedEvent<RecordStatus, StatusRecorded>(_id, @event => true);
            [Fact] async Task should_have_appended_status_recorded_with_status() => await _scenario.ShouldHaveAppendedEvent<RecordStatus, StatusRecorded>(_id, @event => @event.Status == _status);

            public void Dispose() => _scenario.Dispose();
        }
        """;

    /// <summary>
    /// Supplies project global imports and the captured command-scenario extension signatures.
    /// </summary>
    public const string Support = """
        global using System;
        global using System.Threading.Tasks;
        global using Cratis.Arc.Commands.ModelBound;
        global using Cratis.Chronicle.Events;
        global using Cratis.Concepts;
        global using Microsoft.AspNetCore.Authorization;

        namespace Microsoft.AspNetCore.Authorization
        {
            public sealed class AllowAnonymousAttribute : Attribute;
        }

        namespace Cratis.Arc.Chronicle.Testing.Commands
        {
            public static class CommandScenarioChronicleAssertionExtensions
            {
                public static Task ShouldHaveAppendedEvent<TCommand, TEvent>(this Cratis.Arc.Testing.Commands.CommandScenario<TCommand> scenario, EventSourceId eventSourceId, Func<TEvent, bool> predicate) => Task.CompletedTask;
                public static void Dispose<TCommand>(this Cratis.Arc.Testing.Commands.CommandScenario<TCommand> scenario) { }
            }
        }
        """;

    public static (string Path, string Text)[] Sources(string scenario = Scenario, string slice = Slice, string common = Common) =>
    [
        ("Library/Common/Types.cs", common),
        ("Library/Invoices/Lifecycle/Record/Record.cs", slice),
        ("Library/Invoices/Lifecycle/Record/when_draft_status.cs", scenario),
        ("Library/Support.cs", Support),
        (IntegrationTesting.Path, IntegrationTesting.Source)
    ];
}
