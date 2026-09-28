// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Http;
using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation;

[Collection("UsesCurrentDirectory")]
public class when_an_authenticated_caller_changes_before_the_policy_verdict : Specification
{
    CommandResult _httpCommand;
    bool _httpQueryAllowed;
    CommandResult _overrideCommand;
    bool _overrideQueryAllowed;
    int _handledBefore;
    int _evaluations;

    async Task Because()
    {
        var builder = ArcApplication.CreateBuilder();
        builder.AddCratisArc();
        builder.Services.AddArcAuthorizationPolicy<CallerPolicy>("Caller");
        builder.Services.AddSingleton(Substitute.For<ICommandKeys>());
        var available = Substitute.For<ITypes>();
        available.All.Returns([typeof(ProtectedCommand)]);
        builder.Services.AddSingleton<ICommandHandlerProviders>(_ =>
        {
            var providers = Substitute.For<IInstancesOf<ICommandHandlerProvider>>();
            providers.GetEnumerator().Returns(_ => new ICommandHandlerProvider[] { new CommandHandlerProvider(available) }.AsEnumerable().GetEnumerator());
            return new CommandHandlerProviders(providers);
        });
        Action? beforeVerdict = null;
        var executionScope = Substitute.For<ICommandExecutionScope>();
        executionScope.When(scope => scope.Begin(Arg.Any<CommandContext>())).Do(_ => beforeVerdict?.Invoke());
        var scopes = Substitute.For<IInstancesOf<ICommandExecutionScope>>();
        scopes.GetEnumerator().Returns(_ => new ICommandExecutionScope[] { executionScope }.AsEnumerable().GetEnumerator());
        builder.Services.AddSingleton(scopes);
        await using var app = builder.Build();
        await using var scope = app.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var requests = app.Services.GetRequiredService<IHttpRequestContextAccessor>();
        var request = Substitute.For<IHttpRequestContext>();
        request.RequestServices.Returns(services);
        var pipeline = app.Services.GetRequiredService<ICommandPipeline>();
        var evaluation = services.GetRequiredService<AuthorizationEvaluation>();
        var target = typeof(ProtectedQuery).GetMethod(nameof(ProtectedQuery.All))!;
        _handledBefore = ProtectedCommand.Handled;
        try
        {
            requests.Current = request;
            request.User = Caller("A");
            beforeVerdict = () => request.User = Caller("B");
            _httpCommand = await pipeline.Execute(new ProtectedCommand(), services);

            request.User = Caller("A");
            var plan = await evaluation.Prepare(target, services, CancellationToken.None);
            request.User = Caller("B");
            _httpQueryAllowed = await EvaluatePrepared(evaluation, plan, target, services);

            requests.Current = null;
            var principalOverride = services.GetRequiredService<ICurrentPrincipalOverride>();
            using (principalOverride.BeginScope(Caller("A")))
            {
                var replacement = new PrincipalReplacement();
                beforeVerdict = () => replacement.Scope = principalOverride.BeginScope(Caller("B"));
                try
                {
                    _overrideCommand = await pipeline.Execute(new ProtectedCommand(), services);
                }
                finally
                {
                    replacement.Scope?.Dispose();
                }

                plan = await evaluation.Prepare(target, services, CancellationToken.None);
                using (principalOverride.BeginScope(Caller("B")))
                {
                    _overrideQueryAllowed = await EvaluatePrepared(evaluation, plan, target, services);
                }
            }
            _evaluations = services.GetRequiredService<CallerPolicy>().Evaluations;
        }
        finally
        {
            requests.Current = null;
        }
    }

    static ClaimsPrincipal Caller(string name) => new(new ClaimsIdentity([new Claim(ClaimTypes.Name, name)], "test"));

    static Task<bool> EvaluatePrepared(AuthorizationEvaluation evaluation, PreparedAuthorization plan, System.Reflection.MethodInfo target, IServiceProvider services) =>
        evaluation.IsAuthorized(target, QueryContext.NotSet with { PreparedAuthorization = plan }, services, CancellationToken.None);

    [Fact] void should_deny_the_command_when_execution_scope_replaces_the_http_caller() => _httpCommand.IsAuthorized.ShouldBeFalse();
    [Fact] void should_deny_the_query_when_the_http_caller_changes_after_prepare() => _httpQueryAllowed.ShouldBeFalse();
    [Fact] void should_deny_the_command_when_execution_scope_overrides_the_caller() => _overrideCommand.IsAuthorized.ShouldBeFalse();
    [Fact] void should_deny_the_query_when_a_scope_overrides_the_caller_after_prepare() => _overrideQueryAllowed.ShouldBeFalse();
    [Fact] void should_not_execute_the_command_handler() => ProtectedCommand.Handled.ShouldEqual(_handledBefore);
    [Fact] void should_not_evaluate_a_policy_for_a_different_execution_caller() => _evaluations.ShouldEqual(0);

    class PrincipalReplacement
    {
        public IDisposable? Scope { get; set; }
    }

    public class CallerPolicy : IAuthorizationPolicy
    {
        public int Evaluations { get; private set; }

        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
        {
            Evaluations++;
            return ValueTask.FromResult(context.Principal.Identity?.Name == "A");
        }
    }

    [Command]
    [Authorize(Policy = "Caller")]
    public record ProtectedCommand
    {
        static int _handled;
        public static int Handled => Volatile.Read(ref _handled);
        public void Handle() => Interlocked.Increment(ref _handled);
    }

    public static class ProtectedQuery
    {
        [Authorize(Policy = "Caller")]
        public static void All() { }
    }
}
