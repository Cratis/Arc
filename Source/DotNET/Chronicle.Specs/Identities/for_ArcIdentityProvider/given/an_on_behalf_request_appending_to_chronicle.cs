// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Authentication;
using Cratis.Arc.Authorization;
using Cratis.Arc.Http;
using Cratis.Arc.Identity;
using Cratis.Chronicle;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;
using Cratis.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using ChronicleIdentityProvider = Cratis.Chronicle.Identities.IIdentityProvider;
using InMemoryClientArtifactsProvider = Cratis.Chronicle.Testing.InMemoryClientArtifactsProvider;

namespace Cratis.Arc.Chronicle.Identities.for_ArcIdentityProvider.given;

/// <summary>
/// An HTTP request forwarded on behalf of a user, authenticated by Arc's own handler, that appends through an event log
/// whose identity provider is Arc's, backed by Chronicle's in-process kernel.
/// </summary>
public class an_on_behalf_request_appending_to_chronicle : Specification
{
    protected const string RepresentedId = "customer-id";
    protected const string RepresentedName = "customer@example.com";
    protected const string ActorId = "support-person-id";
    protected const string ActorName = "support@cratis.io";

    protected EventStoreForTesting _store;
    protected IEventLog _eventLog;
    protected AuthenticationResult _authentication;
    IHttpRequestContext _request;

    async Task Establish()
    {
        var represented = Principal(RepresentedId, RepresentedName, "customer-admin");
        var actor = Principal(ActorId, ActorName, "support");
        var claims = OnBehalfOfAssertionsForSpecs.ClaimsFor(represented, actor, RepresentedId, ActorId);
        _request = Substitute.For<IHttpRequestContext>();
        _request.Items.Returns(new Dictionary<object, object?>());
        _request.Method.Returns("POST");
        _request.Host.Returns("app.example.com");
        _request.Path.Returns("/api/tickets/close");
        _request.Headers.Returns(new Dictionary<string, string>
        {
            [MicrosoftIdentityPlatformHeaders.IdentityIdHeader] = RepresentedId,
            [MicrosoftIdentityPlatformHeaders.IdentityNameHeader] = RepresentedName,
            [MicrosoftIdentityPlatformHeaders.PrincipalHeader] = represented,
            [OnBehalfOfHeaders.ActorHeader] = actor,
            [OnBehalfOfHeaders.AssertionHeader] = OnBehalfOfAssertionsForSpecs.Sign(claims)
        });

        var options = new ArcOptions { TrustForwardedIdentityHeaders = true, OnBehalfOf = OnBehalfOfAssertionsForSpecs.Options() };
        var handler = new MicrosoftIdentityPlatformAuthenticationHandler(Options.Create(options), NullLoggerFactory.Instance);
        _authentication = await handler.HandleAuthentication(_request);
        _request.User.Returns(_authentication.Principal);

        var requests = Substitute.For<IHttpRequestContextAccessor>();
        requests.Current.Returns(_request);
        var identities = new ArcIdentityProvider(new BaseIdentityProvider(), new CurrentPrincipalAccessor(requests), () => requests.Current is not null);

        _store = new EventStoreForTesting(null, new InMemoryClientArtifactsProvider { EventTypes = [typeof(TicketClosed)] });
        _eventLog = EventLogWith(identities);
    }

    void Destroy() => new BaseIdentityProvider().ClearCurrentIdentity();

    static string Principal(string userId, string userDetails, string role) =>
        Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(new
        {
            identityProvider = "aad",
            userId,
            userDetails,
            userRoles = new[] { role },
            claims = new[] { new { typ = "name", val = userDetails } }
        }));

    /// <summary>
    /// Builds the client event log the way a Chronicle client does, with Arc's identity provider, over the in-process kernel.
    /// </summary>
    /// <param name="identities">The <see cref="ChronicleIdentityProvider"/> to resolve the caused-by identity with.</param>
    /// <returns>The <see cref="IEventLog"/>.</returns>
    IEventLog EventLogWith(ChronicleIdentityProvider identities)
    {
        var json = Globals.JsonSerializerOptions ?? new JsonSerializerOptions();
        var concurrency = new ConcurrencyScopeStrategies(new ConcurrencyOptions(), new ServiceCollection().BuildServiceProvider());
        var units = Substitute.For<IUnitOfWorkManager>();
        units.HasCurrent.Returns(false);

        return new EventLog(
            _store.Name,
            _store.Namespace,
            _store.Connection,
            _store.EventTypes,
            _store.Constraints,
            new EventSerializer(new InMemoryClientArtifactsProvider { EventTypes = [typeof(TicketClosed)] }, Substitute.For<IClientArtifactsActivator>(), _store.EventTypes, json),
            new CorrelationIdAccessor(),
            concurrency,
            new CausationManager(),
            units,
            identities,
            json,
            null,
            _store.EventSources);
    }
}
