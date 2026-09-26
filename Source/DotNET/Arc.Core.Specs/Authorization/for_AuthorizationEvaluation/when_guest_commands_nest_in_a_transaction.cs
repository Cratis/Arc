// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation;

[Collection("UsesCurrentDirectory")]
public class when_guest_commands_nest_in_a_transaction : Specification
{
    CommandResult _guestOuter;
    CommandResult _unprotectedOuter;

    async Task Because()
    {
        var builder = ArcApplication.CreateBuilder();
        builder.AddCratisArc();
        builder.Services.AddArcAuthorizationPolicy<AllowGuest>("AllowGuest", evaluatesAnonymous: true);
        builder.Services.AddSingleton(Substitute.For<ICommandKeys>());
        var available = Substitute.For<ITypes>();
        available.All.Returns([typeof(GuestOuter), typeof(UnprotectedOuter), typeof(GuestInner), typeof(UnprotectedInner)]);
        builder.Services.AddSingleton<ICommandHandlerProviders>(_ =>
        {
            var providers = Substitute.For<IInstancesOf<ICommandHandlerProvider>>();
            providers.GetEnumerator().Returns(_ => new ICommandHandlerProvider[] { new CommandHandlerProvider(available) }.AsEnumerable().GetEnumerator());
            return new CommandHandlerProviders(providers);
        });
        await using var app = builder.Build();
        using var guest = app.Services.GetRequiredService<ISystemExecution>().As(
            new ClaimsPrincipal(new ClaimsIdentity([new Claim("guest", "ambient")])));
        await using var scope = app.Services.CreateAsyncScope();
        var pipeline = app.Services.GetRequiredService<ICommandPipeline>();
        _guestOuter = await pipeline.Execute(new GuestOuter(), scope.ServiceProvider);
        _unprotectedOuter = await pipeline.Execute(new UnprotectedOuter(), scope.ServiceProvider);
    }

    [Fact] void should_allow_guest_outer_to_nest_unprotected() => GuestOuter.ChildResult.IsAuthorized.ShouldBeTrue();
    [Fact] void should_allow_unprotected_outer_to_nest_guest() => UnprotectedOuter.ChildResult.IsAuthorized.ShouldBeTrue();
    [Fact] void should_preserve_guest_outer_identity() => GuestOuter.RestoredClaim.ShouldEqual("ambient");
    [Fact] void should_preserve_unprotected_outer_identity() => UnprotectedOuter.RestoredClaim.ShouldEqual("ambient");

    public class AllowGuest : IAuthorizationPolicy
    {
        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken) => ValueTask.FromResult(true);
    }

    [Command]
    [Authorize(Policy = "AllowGuest")]
    public record GuestOuter
    {
        public static string? RestoredClaim { get; private set; }
        public static CommandResult ChildResult { get; private set; } = null!;

        public async Task Handle(ICommandPipeline pipeline, IServiceProvider services, ICurrentPrincipalAccessor principal)
        {
            AuthorizationCommandIdentity.MarkTransactional();
            ChildResult = await pipeline.Execute(new UnprotectedInner(), services);
            RestoredClaim = principal.Current?.FindFirst("guest")?.Value;
        }
    }

    [Command]
    public record UnprotectedOuter
    {
        public static string? RestoredClaim { get; private set; }
        public static CommandResult ChildResult { get; private set; } = null!;

        public async Task Handle(ICommandPipeline pipeline, IServiceProvider services, ICurrentPrincipalAccessor principal)
        {
            AuthorizationCommandIdentity.MarkTransactional();
            ChildResult = await pipeline.Execute(new GuestInner(), services);
            RestoredClaim = principal.Current?.FindFirst("guest")?.Value;
        }
    }

    [Command]
    [Authorize(Policy = "AllowGuest")]
    public record GuestInner
    {
        public void Handle() { }
    }

    [Command]
    public record UnprotectedInner
    {
        public void Handle() { }
    }
}
