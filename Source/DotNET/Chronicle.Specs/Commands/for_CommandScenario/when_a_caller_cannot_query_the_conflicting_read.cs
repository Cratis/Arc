// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Arc.Authorization;
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Queries;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Testing.Queries;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario;

public class when_a_caller_cannot_query_the_conflicting_read : Specification
{
    const string HiddenKey = "server-selected-stock";
    CommandScenario<Borrow> _command;
    QueryScenario<RestrictedStock> _query;
    CommandResult _result;
    QueryResult _queryResult;

    void Establish()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "borrower")], "test"));
        var accessor = Substitute.For<ICurrentPrincipalAccessor>();
        accessor.Current.Returns(principal);
        _command = new();
        _command.Services.AddSingleton(accessor);
        _query = new();
        _query.Services.AddSingleton(accessor);
        _command.Given.ForEventSource(HiddenKey).Events(new SecretStockChanged());
        _command.AppendConcurrently(HiddenKey, new SecretStockChanged());
    }

    async Task Because()
    {
        _queryResult = await _query.Perform(nameof(RestrictedStock.Current), cancellationToken: global::Xunit.TestContext.Current.CancellationToken);
        _result = await _command.Execute(new(EventSourceId.New()), global::Xunit.TestContext.Current.CancellationToken);
    }

    [Fact] void should_deny_the_same_principal_access_to_the_read_model() => _queryResult.IsAuthorized.ShouldBeFalse();
    [Fact] void should_allow_the_command() => _result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_not_fail_with_an_exception() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_report_the_read_changed_conflict() => _result.ShouldHaveValidationErrorBecauseOf(ValidationResultReason.ConcurrencyViolation);
    [Fact] void should_preserve_the_reason_detail() => _result.ValidationResults.Single().ReasonDetail.ShouldEqual("ReadChanged");
    [Fact] void should_not_commit_the_owner_event() => _command.AppendedEvents.ShouldBeEmpty();
    [Fact] void should_only_describe_the_changed_read() => _result.ValidationResults.Single().Message.ShouldEqual(
        "RestrictedStock changed after Borrow read it. Nothing from this command was written; submit it again.");

    [Fact]
    void should_only_identify_the_read_model_in_state()
    {
        var state = JsonSerializer.SerializeToElement(_result.ValidationResults.Single().State);
        state.EnumerateObject().Select(_ => _.Name).ShouldContainOnly("readModel");
        state.GetProperty("readModel").GetString().ShouldEqual(nameof(RestrictedStock));
    }

    [Fact]
    void should_not_disclose_the_provider_key_or_conflicting_event_in_the_serialized_result()
    {
        var json = JsonSerializer.Serialize(_result);
        json.ShouldNotContain(HiddenKey);
        json.ShouldNotContain(nameof(SecretStockChanged));
        json.ShouldNotContain("conflictingEvent");
    }

    async Task Destroy()
    {
        await _command.DisposeAsync();
        await _query.DisposeAsync();
    }

    [Command, Authorize(Roles = "borrower"), ReadProtection(ReadProtectionMode.On)]
    public record Borrow(EventSourceId Id)
    {
        public Borrowed Handle([LookUpBy<ServerSelectedStock>] RestrictedStock stock) => new(stock.Id == HiddenKey);
    }

    public sealed class ServerSelectedStock : IReadModelKeyProvider
    {
        public ReadModelKeyLookup Resolve(ReadModelKeyContext context) => ReadModelKeyLookup.Found(HiddenKey);
    }

    [ReadModel, FromEvent<SecretStockChanged>]
    public record RestrictedStock([property: Key] string Id)
    {
        [Authorize(Roles = "stock-reader")]
        public static RestrictedStock Current() => new(HiddenKey);
    }

    [EventType]
    public record SecretStockChanged;

    [EventType]
    public record Borrowed(bool WasPresent);
}
