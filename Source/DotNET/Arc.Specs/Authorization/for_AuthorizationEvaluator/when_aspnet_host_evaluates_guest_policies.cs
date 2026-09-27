// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Queries;
using Cratis.Arc.Queries.ModelBound;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluator;

[Collection("UsesCurrentDirectory")]
public class when_aspnet_host_evaluates_guest_policies : Specification
{
    CommandResult _arcGuest;
    CommandResult _aspGuest;
    CommandResult _aspRegistered;
    QueryResult _query;
    int _policyCalls;

    async Task Because()
    {
        GuestPolicy.Calls = 0;
        var builder = WebApplication.CreateBuilder();
        builder.AddCratisArc();
        builder.Services.AddArcAuthorizationPolicy<GuestPolicy>("Guest", evaluatesAnonymous: true);
        builder.Services.AddAuthorizationBuilder().AddPolicy("AspRegistered", policy => policy.RequireAssertion(_ => true));
        builder.Services.AddSingleton(Substitute.For<ICommandKeys>());
        var available = Substitute.For<ITypes>();
        available.All.Returns([typeof(ArcGuestCommand), typeof(AspGuestCommand), typeof(AspRegisteredCommand), typeof(GuestReadModel)]);
        builder.Services.AddSingleton<ICommandHandlerProviders>(_ =>
        {
            var providers = Substitute.For<IInstancesOf<ICommandHandlerProvider>>();
            providers.GetEnumerator().Returns(_ => new ICommandHandlerProvider[] { new CommandHandlerProvider(available) }.AsEnumerable().GetEnumerator());
            return new CommandHandlerProviders(providers);
        });
        builder.Services.AddSingleton<IQueryPerformerProviders>(services =>
        {
            var metadata = Substitute.For<IQueryMetadataRegistry>();
            metadata.All.Returns(new Dictionary<string, Type> { [$"{typeof(GuestReadModel).FullName}.All"] = typeof(GuestReadModel) });
            var provider = new QueryPerformerProvider(
                available,
                metadata,
                services.GetRequiredService<IServiceProviderIsService>(),
                services.GetRequiredService<IAuthorizationEvaluator>());
            var providers = Substitute.For<IInstancesOf<IQueryPerformerProvider>>();
            providers.GetEnumerator().Returns(_ => new IQueryPerformerProvider[] { provider }.AsEnumerable().GetEnumerator());
            return new QueryPerformerProviders(providers);
        });
        await using var app = builder.Build();
        var commands = app.Services.GetRequiredService<ICommandPipeline>();
        var queries = app.Services.GetRequiredService<QueryPipeline>();
        var execution = app.Services.GetRequiredService<ISystemExecution>();
        using (execution.As(new ClaimsPrincipal(new ClaimsIdentity())))
        {
            _arcGuest = await commands.Execute(new ArcGuestCommand());
            _aspGuest = await commands.Execute(new AspGuestCommand());
            _aspRegistered = await commands.Execute(new AspRegisteredCommand());
            _query = await queries.PerformHosted(
                new FullyQualifiedQueryName($"{typeof(GuestReadModel).FullName}.All"),
                QueryArguments.Empty,
                Paging.NotPaged,
                Sorting.None,
                app.Services,
                CancellationToken.None);
        }

        _policyCalls = GuestPolicy.Calls;
    }

    [Fact] void should_allow_the_arc_attribute() => _arcGuest.IsAuthorized.ShouldBeTrue();
    [Fact] void should_allow_the_aspnet_attribute() => _aspGuest.IsAuthorized.ShouldBeTrue();
    [Fact] void should_deny_the_aspnet_registered_policy_for_guests() => _aspRegistered.IsAuthorized.ShouldBeFalse();
    [Fact] void should_evaluate_a_policy_on_a_method_overriding_an_anonymous_type() => _query.IsAuthorized.ShouldBeTrue();
    [Fact] void should_evaluate_the_policy_for_both_commands_and_the_query() => _policyCalls.ShouldEqual(3);

    [Command]
    [Authorize(Policy = "Guest")]
    private record ArcGuestCommand { public void Handle() { } }

    [Command]
    [Microsoft.AspNetCore.Authorization.Authorize(Policy = "Guest")]
    private record AspGuestCommand { public void Handle() { } }

    [Command]
    [Microsoft.AspNetCore.Authorization.Authorize(Policy = "AspRegistered")]
    private record AspRegisteredCommand { public void Handle() { } }

    [ReadModel]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    private record GuestReadModel(string Value)
    {
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = "Guest")]
        public static GuestReadModel All() => new("allowed");
    }

    public class GuestPolicy : IAuthorizationPolicy
    {
        public static int Calls;
        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref Calls);
            return ValueTask.FromResult(context.Principal.Identity?.IsAuthenticated == false && !context.Principal.HasClaim("untrusted", "ignored"));
        }
    }
}
