// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text.Json;
using Cratis.Arc.Http;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_get_context_is_disposed_while_query_is_performed : given.a_guarded_connection
{
    readonly ClaimsPrincipal _postPrincipal = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "fresh-caller")], "test"));
    readonly ConcurrentQueue<string> _messages = [];
    readonly TaskCompletionSource<QueryResult> _performCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _performStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly CancellationTokenSource _connectionCancellation = new();
    readonly ConcurrentQueue<(string? User, string? Tenant)> _ambientGuardContexts = [];
    readonly ConcurrentQueue<(string? User, string? Tenant)> _ambientInterceptorContexts = [];
    IHttpRequestContext _authorizationContext;
    IHttpRequestContext _connectionContext;
    IHttpRequestContext _subscribeContext;
    string _connectionId;
    bool _getContextDisposed;
    int _getUserReads;
    int _getUserWrites;
    int _subscribeStatusCode;

    void Establish()
    {
        _connectionId = string.Empty;

        _queryPipeline.Perform(Arg.Any<FullyQualifiedQueryName>(), Arg.Any<QueryArguments>(), Arg.Any<Paging>(), Arg.Any<Sorting>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                _authorizationContext = _httpRequestContextAccessor.Current;
                _authorizationContext.User.AddIdentity(new ClaimsIdentity([new Claim(ClaimTypes.Name, "mutation-attempt")], "test"));
                _authorizationContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "replacement-attempt")], "test"));
                _performStarted.TrySetResult();
                return _performCompletion.Task;
            });

        _connectionContext = Substitute.For<IHttpRequestContext>();
        _connectionContext.RequestAborted.Returns(_connectionCancellation.Token);
        _connectionContext.RequestServices.Returns(Substitute.For<IServiceProvider>());
        _connectionContext.Headers.Returns(new Dictionary<string, string> { ["Tenant-ID"] = "tenant-a" });
        _connectionContext.User.Returns(_ =>
        {
            _getUserReads++;
            ObjectDisposedException.ThrowIf(_getContextDisposed, _connectionContext);

            return new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "stale-caller")], "test"));
        });
        _connectionContext.When(_ => _.User = Arg.Any<ClaimsPrincipal>())
            .Do(_ =>
            {
                _getUserWrites++;
                ObjectDisposedException.ThrowIf(_getContextDisposed, _connectionContext);
            });
        _connectionContext.Write(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                _messages.Enqueue(callInfo.Arg<string>());
                return Task.CompletedTask;
            });

        _subscribeContext = Substitute.For<IHttpRequestContext>();
        _subscribeContext.Headers.Returns(new Dictionary<string, string> { ["Tenant-ID"] = "tenant-b" });
        _subscribeContext.RequestAborted.Returns(CancellationToken.None);
        _subscribeContext.RequestServices.Returns(Substitute.For<IServiceProvider>());
        _subscribeContext.User.Returns(_postPrincipal);
        _subscribeContext.ReadBodyAsJson(typeof(ObservableQuerySSESubscribeRequest), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<object?>(new ObservableQuerySSESubscribeRequest(
                _connectionId,
                FirstQueryId,
                new ObservableQuerySubscriptionRequest(QueryName))));
        _subscribeContext.When(_ => _.SetStatusCode(Arg.Any<int>()))
            .Do(callInfo => _subscribeStatusCode = callInfo.Arg<int>());

        _readModelInterceptors.Intercept(Arg.Any<Type>(), Arg.Any<IEnumerable<object>>(), Arg.Any<IServiceProvider>())
            .Returns(callInfo =>
            {
                var ambient = _httpRequestContextAccessor.Current;
                _ambientInterceptorContexts.Enqueue((ambient?.User.Identity?.Name, ambient?.Headers.GetValueOrDefault("Tenant-ID")));
                return Task.FromResult(callInfo.ArgAt<IEnumerable<object>>(1));
            });
    }

    async Task Because()
    {
        var connectionTask = _hub.HandleSSEConnection(_connectionContext);
        await WaitFor(() => TryExtractConnectionId(out _connectionId));

        try
        {
            var subscribeTask = _hub.HandleSSESubscribe(_subscribeContext);
            await _performStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
            _postPrincipal.AddIdentity(new ClaimsIdentity([new Claim(ClaimTypes.Name, "late-mutation")]));

            _getContextDisposed = true;
            var queryResult = QueryResult.Success(CorrelationId.New());
            queryResult.Data = _subject;
            _performCompletion.TrySetResult(queryResult);

            await subscribeTask;
            _subject.OnNext(["event-store-a"]);
            await WaitFor(() => _guardCalls.Count == 1);
        }
        finally
        {
            await _connectionCancellation.CancelAsync();
            await connectionTask;
        }
    }

    [Fact] void should_complete_the_subscribe() => _subscribeStatusCode.ShouldEqual(200);
    [Fact] void should_authorize_with_a_durable_snapshot_instead_of_the_post_context() => ReferenceEquals(_authorizationContext, _subscribeContext).ShouldBeFalse();
    [Fact] void should_ignore_user_replacement_attempts_during_the_pipeline() => _authorizationContext.User.Identity?.Name.ShouldEqual("fresh-caller");
    [Fact] void should_authorize_with_the_post_tenant_snapshot() => _authorizationContext.Headers["Tenant-ID"].ShouldEqual("tenant-b");
    [Fact] void should_not_read_user_from_the_get_context() => _getUserReads.ShouldEqual(0);
    [Fact] void should_not_write_user_to_the_get_context() => _getUserWrites.ShouldEqual(0);
    [Fact] void should_consult_the_emission_guard() => _guardCalls.Count.ShouldEqual(1);
    [Fact] void should_give_the_emission_guard_the_post_principal() => _guardCalls.Single().Principal?.Identity?.Name.ShouldEqual("fresh-caller");
    [Fact] void should_restore_the_post_context_for_injected_guard_dependencies() => _ambientGuardContexts.Single().ShouldEqual(("fresh-caller", "tenant-b"));
    [Fact] void should_restore_the_post_context_for_emission_interceptors() => _ambientInterceptorContexts.Single().ShouldEqual(("fresh-caller", "tenant-b"));

    protected override void ConfigureGuards(IServiceCollection services, List<Type> guardTypes)
    {
        services.AddSingleton(_httpRequestContextAccessor);
        services.AddSingleton(_ambientGuardContexts);
        guardTypes.Add(typeof(AmbientContextGuard));
    }

    public class AmbientContextGuard(
        IHttpRequestContextAccessor accessor,
        ConcurrentQueue<(string? User, string? Tenant)> contexts) : IGuardObservableQueryEmission
    {
        public Task<ObservableQueryEmissionVerdict> Guard(ObservableQueryEmissionContext context)
        {
            var ambient = accessor.Current;
            contexts.Enqueue((ambient?.User.Identity?.Name, ambient?.Headers.GetValueOrDefault("Tenant-ID")));
            return Task.FromResult(ObservableQueryEmissionVerdict.Allow);
        }
    }

    bool TryExtractConnectionId(out string connectionId)
    {
        connectionId = string.Empty;

        foreach (var hubMessage in _messages.Select(TryParseHubMessage).Where(_ => _ is not null).Select(_ => _!))
        {
            if (hubMessage.Type != ObservableQueryHubMessageType.Connected || hubMessage.Payload is not JsonElement payload)
            {
                continue;
            }

            connectionId = payload.GetString() ?? string.Empty;
            return !string.IsNullOrEmpty(connectionId);
        }

        return false;
    }

    ObservableQueryHubMessage? TryParseHubMessage(string sseMessage)
    {
        if (!sseMessage.StartsWith("data: ", StringComparison.Ordinal))
        {
            return null;
        }

        var json = sseMessage["data: ".Length..].Trim();
        return JsonSerializer.Deserialize<ObservableQueryHubMessage>(json, _arcOptions.Value.JsonSerializerOptions);
    }
}
