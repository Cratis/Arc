// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation;

[Collection("UsesCurrentDirectory")]
public class when_a_guest_caller_changes_before_the_policy_verdict : Specification
{
    CommandResult _replaced;
    CommandResult _authenticatedIdentity;
    CommandResult _unauthenticatedEnrichment;
    bool _replacedQueryAllowed;
    bool _authenticatedQueryAllowed;
    bool _enrichedQueryAllowed;
    int _handledBefore;
    int _evaluations;

    async Task Because()
    {
        var builder = ArcApplication.CreateBuilder();
        builder.AddCratisArc();
        builder.Services.AddArcAuthorizationPolicy<GuestPolicy>("Guest", evaluatesAnonymous: true);
        builder.Services.AddSingleton(Substitute.For<ICommandKeys>());
        var available = Substitute.For<ITypes>();
        available.All.Returns([typeof(GuestCommand)]);
        builder.Services.AddSingleton<ICommandHandlerProviders>(_ =>
        {
            var providers = Substitute.For<IInstancesOf<ICommandHandlerProvider>>();
            providers.GetEnumerator().Returns(_ => new ICommandHandlerProvider[] { new CommandHandlerProvider(available) }.AsEnumerable().GetEnumerator());
            return new CommandHandlerProviders(providers);
        });
        var change = new PrincipalChange();
        var executionScope = Substitute.For<ICommandExecutionScope>();
        executionScope.When(scope => scope.Begin(Arg.Any<CommandContext>())).Do(_ => change.Apply?.Invoke());
        var scopes = Substitute.For<IInstancesOf<ICommandExecutionScope>>();
        scopes.GetEnumerator().Returns(_ => new ICommandExecutionScope[] { executionScope }.AsEnumerable().GetEnumerator());
        builder.Services.AddSingleton(scopes);
        await using var app = builder.Build();
        var requests = app.Services.GetRequiredService<IHttpRequestContextAccessor>();
        await using var scope = app.Services.CreateAsyncScope();
        var request = Substitute.For<IHttpRequestContext>();
        request.RequestServices.Returns(scope.ServiceProvider);
        requests.Current = request;
        var pipeline = app.Services.GetRequiredService<ICommandPipeline>();
        _handledBefore = GuestCommand.Handled;
        try
        {
            request.User = new ClaimsPrincipal(new ClaimsIdentity());
            change.Apply = () => request.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "authenticated")], "test"));
            _replaced = await pipeline.Execute(new GuestCommand(), scope.ServiceProvider);

            request.User = new ClaimsPrincipal();
            change.Apply = () => request.User.AddIdentity(new ClaimsIdentity([new Claim(ClaimTypes.Name, "authenticated")], "test"));
            _authenticatedIdentity = await pipeline.Execute(new GuestCommand(), scope.ServiceProvider);

            request.User = new ClaimsPrincipal(new ClaimsIdentity());
            change.Apply = () => request.User.AddIdentity(new ClaimsIdentity([new Claim("guest", "enriched")]));
            _unauthenticatedEnrichment = await pipeline.Execute(new GuestCommand(), scope.ServiceProvider);

            var evaluation = scope.ServiceProvider.GetRequiredService<AuthorizationEvaluation>();
            var target = typeof(GuestQuery).GetMethod(nameof(GuestQuery.All))!;
            request.User = new ClaimsPrincipal(new ClaimsIdentity());
            var plan = await evaluation.Prepare(target, scope.ServiceProvider, CancellationToken.None);
            request.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "authenticated")], "test"));
            _replacedQueryAllowed = await EvaluatePrepared(evaluation, plan, target, scope.ServiceProvider);

            request.User = new ClaimsPrincipal();
            plan = await evaluation.Prepare(target, scope.ServiceProvider, CancellationToken.None);
            request.User.AddIdentity(new ClaimsIdentity([new Claim(ClaimTypes.Name, "authenticated")], "test"));
            _authenticatedQueryAllowed = await EvaluatePrepared(evaluation, plan, target, scope.ServiceProvider);

            request.User = new ClaimsPrincipal(new ClaimsIdentity());
            plan = await evaluation.Prepare(target, scope.ServiceProvider, CancellationToken.None);
            request.User.AddIdentity(new ClaimsIdentity([new Claim("guest", "enriched")]));
            _enrichedQueryAllowed = await EvaluatePrepared(evaluation, plan, target, scope.ServiceProvider);
            _evaluations = scope.ServiceProvider.GetRequiredService<GuestPolicy>().Evaluations;
        }
        finally
        {
            requests.Current = null;
        }
    }

    static Task<bool> EvaluatePrepared(AuthorizationEvaluation evaluation, PreparedAuthorization plan, System.Reflection.MethodInfo target, IServiceProvider services) =>
        evaluation.IsAuthorized(target, Cratis.Arc.Queries.QueryContext.NotSet with { PreparedAuthorization = plan }, services, CancellationToken.None);

    [Fact] void should_deny_a_replaced_authenticated_command_caller() => _replaced.IsAuthorized.ShouldBeFalse();
    [Fact] void should_deny_a_new_authenticated_command_identity() => _authenticatedIdentity.IsAuthorized.ShouldBeFalse();
    [Fact] void should_allow_unauthenticated_command_enrichment() => _unauthenticatedEnrichment.IsAuthorized.ShouldBeTrue();
    [Fact] void should_execute_only_the_enriched_command() => GuestCommand.Handled.ShouldEqual(_handledBefore + 1);
    [Fact] void should_deny_a_replaced_authenticated_query_caller() => _replacedQueryAllowed.ShouldBeFalse();
    [Fact] void should_deny_a_new_authenticated_query_identity() => _authenticatedQueryAllowed.ShouldBeFalse();
    [Fact] void should_allow_unauthenticated_query_enrichment() => _enrichedQueryAllowed.ShouldBeTrue();
    [Fact] void should_evaluate_only_unauthenticated_callers() => _evaluations.ShouldEqual(2);

    class PrincipalChange
    {
        public Action? Apply { get; set; }
    }

    public class GuestPolicy : IAuthorizationPolicy
    {
        public int Evaluations { get; private set; }

        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
        {
            Evaluations++;
            return ValueTask.FromResult(!context.Principal.Identity!.IsAuthenticated);
        }
    }

    [Command]
    [Authorize(Policy = "Guest")]
    public record GuestCommand
    {
        static int _handled;
        public static int Handled => Volatile.Read(ref _handled);
        public void Handle() => Interlocked.Increment(ref _handled);
    }

    public static class GuestQuery
    {
        [Authorize(Policy = "Guest")]
        public static void All() { }
    }
}
