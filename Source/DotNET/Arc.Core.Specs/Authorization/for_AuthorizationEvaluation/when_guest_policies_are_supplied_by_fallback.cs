// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Claims;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation;

[Collection("UsesCurrentDirectory")]
public class when_guest_policies_are_supplied_by_fallback : Specification
{
    CommandResult _bare;
    CommandResult _role;
    CommandResult _defaultPolicy;
    CommandResult _publicPolicy;
    int _defaultCalls;
    int _publicCalls;

    async Task Because()
    {
        DefaultPolicy.Calls = 0;
        PublicPolicy.Calls = 0;
        var builder = ArcApplication.CreateBuilder();
        builder.AddCratisArc();
        builder.Services.AddArcAuthorizationPolicy<DefaultPolicy>("Default");
        builder.Services.AddArcAuthorizationPolicy<PublicPolicy>("Public", evaluatesAnonymous: true);
        builder.Services.AddSingleton(Substitute.For<ICommandKeys>());
        var available = Substitute.For<ITypes>();
        available.All.Returns([typeof(BareCommand), typeof(RoleCommand), typeof(DefaultCommand), typeof(PublicCommand)]);
        builder.Services.AddSingleton<ICommandHandlerProviders>(_ =>
        {
            var providers = Substitute.For<IInstancesOf<ICommandHandlerProvider>>();
            providers.GetEnumerator().Returns(_ => new ICommandHandlerProvider[] { new CommandHandlerProvider(available) }.AsEnumerable().GetEnumerator());
            return new CommandHandlerProviders(providers);
        });
        await using var app = builder.Build();
        var commands = app.Services.GetRequiredService<ICommandPipeline>();
        var execution = app.Services.GetRequiredService<ISystemExecution>();
        await using var scope = app.Services.CreateAsyncScope();
        using (execution.As(new ClaimsPrincipal(new ClaimsIdentity())))
        {
            _bare = await commands.Execute(new BareCommand(), scope.ServiceProvider);
            _role = await commands.Execute(new RoleCommand(), scope.ServiceProvider);
            _defaultPolicy = await commands.Execute(new DefaultCommand(), scope.ServiceProvider);
            _publicPolicy = await commands.Execute(new PublicCommand(), scope.ServiceProvider);
        }

        _defaultCalls = DefaultPolicy.Calls;
        _publicCalls = PublicPolicy.Calls;
    }

    [Fact] void should_deny_a_bare_authorize_fallback() => _bare.IsAuthorized.ShouldBeFalse();
    [Fact] void should_deny_a_role_fallback() => _role.IsAuthorized.ShouldBeFalse();
    [Fact] void should_deny_a_non_opted_in_policy_fallback() => _defaultPolicy.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_run_a_non_opted_in_policy_for_a_guest() => _defaultCalls.ShouldEqual(0);
    [Fact] void should_allow_a_guest_through_an_opted_in_policy_fallback() => _publicPolicy.IsAuthorized.ShouldBeTrue();
    [Fact] void should_run_the_opted_in_policy_once() => _publicCalls.ShouldEqual(1);

    [Command] public record BareCommand { public void Handle() { } }
    [Command] public record RoleCommand { public void Handle() { } }
    [Command] public record DefaultCommand { public void Handle() { } }
    [Command] public record PublicCommand { public void Handle() { } }

    public class GuestBaseline : IFallbackAuthorizationEvaluator
    {
        public IEnumerable<AuthorizationRequirement> GetAuthorizationRequirements(Type type) => type switch
        {
            _ when type == typeof(BareCommand) => [AuthorizationRequirement.FromRoles(null)],
            _ when type == typeof(RoleCommand) => [AuthorizationRequirement.FromRoles("Admin")],
            _ when type == typeof(DefaultCommand) => [AuthorizationRequirement.FromAttribute(null, "Default", null)],
            _ when type == typeof(PublicCommand) => [AuthorizationRequirement.FromAttribute(null, "Public", null)],
            _ => []
        };

        public IEnumerable<AuthorizationRequirement> GetAuthorizationRequirements(MethodInfo method) => [];
    }

    public class DefaultPolicy : IAuthorizationPolicy
    {
        public static int Calls;
        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref Calls);
            return ValueTask.FromResult(true);
        }
    }

    public class PublicPolicy : IAuthorizationPolicy
    {
        public static int Calls;
        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref Calls);
            return ValueTask.FromResult(!context.Principal.Identities.Any(identity => identity.IsAuthenticated));
        }
    }
}
