// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation;

[Collection("UsesCurrentDirectory")]
public class when_a_command_scope_adds_claims_before_the_policy_verdict : Specification
{
    CommandResult _result;
    bool _policySawClaim;
    int _handledBefore;

    async Task Because()
    {
        var builder = ArcApplication.CreateBuilder();
        builder.AddCratisArc();
        builder.Services.AddArcAuthorizationPolicy<ClaimPolicy>("Tenant");
        builder.Services.AddSingleton(Substitute.For<ICommandKeys>());
        var available = Substitute.For<ITypes>();
        available.All.Returns([typeof(TenantCommand)]);
        builder.Services.AddSingleton<ICommandHandlerProviders>(_ =>
        {
            var providers = Substitute.For<IInstancesOf<ICommandHandlerProvider>>();
            providers.GetEnumerator().Returns(_ => new ICommandHandlerProvider[] { new CommandHandlerProvider(available) }.AsEnumerable().GetEnumerator());
            return new CommandHandlerProviders(providers);
        });
        var executionScope = Substitute.For<ICommandExecutionScope>();
        executionScope.When(scope => scope.Begin(Arg.Any<CommandContext>())).Do(call =>
        {
            var principal = call.Arg<CommandContext>().ServiceProvider.GetRequiredService<ICurrentPrincipalAccessor>().Current!;
            principal.AddIdentity(new ClaimsIdentity([new Claim("tenant", "allowed")]));
        });
        var scopes = Substitute.For<IInstancesOf<ICommandExecutionScope>>();
        scopes.GetEnumerator().Returns(_ => new ICommandExecutionScope[] { executionScope }.AsEnumerable().GetEnumerator());
        builder.Services.AddSingleton(scopes);
        await using var app = builder.Build();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "caller")], "test"));
        _handledBefore = TenantCommand.Handled;
        var requests = app.Services.GetRequiredService<IHttpRequestContextAccessor>();
        await using var scope = app.Services.CreateAsyncScope();
        var request = Substitute.For<IHttpRequestContext>();
        request.RequestServices.Returns(scope.ServiceProvider);
        request.User = principal;
        requests.Current = request;
        try
        {
            _result = await app.Services.GetRequiredService<ICommandPipeline>().Execute(new TenantCommand(), scope.ServiceProvider);
            _policySawClaim = scope.ServiceProvider.GetRequiredService<ClaimPolicy>().SawClaim;
        }
        finally
        {
            requests.Current = null;
        }
    }

    [Fact] void should_have_no_errors() => _result.ExceptionMessages.ShouldBeEmpty();
    [Fact] void should_present_the_claim_to_the_policy() => _policySawClaim.ShouldBeTrue();
    [Fact] void should_authorize_the_command() => _result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_execute_the_handler() => TenantCommand.Handled.ShouldEqual(_handledBefore + 1);

    public class ClaimPolicy : IAuthorizationPolicy
    {
        public bool SawClaim { get; private set; }

        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
        {
            SawClaim = context.Principal.HasClaim("tenant", "allowed");
            return ValueTask.FromResult(SawClaim);
        }
    }

    [Command]
    [Authorize(Policy = "Tenant")]
    public record TenantCommand
    {
        static int _handled;
        public static int Handled => Volatile.Read(ref _handled);
        public void Handle() => Interlocked.Increment(ref _handled);
    }
}
