// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.Authorization.for_AuthorizationStartupValidation;

[Collection("UsesCurrentDirectory")]
public class when_a_concurrent_generic_host_starts_after_arc : Specification
{
    Exception? _failure;
    bool _listenerPortWasUnboundDuringValidation;

    async Task Because()
    {
        var portProbe = new TcpListener(IPAddress.Loopback, 0);
        portProbe.Start();
        var port = ((IPEndPoint)portProbe.LocalEndpoint).Port;
        portProbe.Stop();

        var policyProvider = new DelayedUnknownPolicyProvider();
        var handlers = Substitute.For<ICommandHandlerProviders>();
        var handler = Substitute.For<ICommandHandler>();
        handler.CommandType.Returns(typeof(MissingPolicyCommand));
        handlers.Handlers.Returns([handler]);
        var performers = Substitute.For<IQueryPerformerProviders>();
        performers.Performers.Returns([]);
        using var host = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(web => web.UseKestrel(options => options.Listen(IPAddress.Loopback, port))
                .Configure(app => app.UseCratisArc()))
            .AddCratisArc()
            .ConfigureServices(services =>
            {
                services.AddSingleton(handlers);
                services.AddSingleton(performers);
                services.AddSingleton<IAuthorizationPolicyProvider>(policyProvider);
                services.Configure<HostOptions>(options => options.ServicesStartConcurrently = true);
            })
            .Build();
        var starting = host.StartAsync();
        try
        {
            await policyProvider.Entered.WaitAsync(TimeSpan.FromSeconds(10));
            var listener = new TcpListener(IPAddress.Loopback, port);
            try
            {
                listener.Start();
                _listenerPortWasUnboundDuringValidation = true;
            }
            catch (SocketException)
            {
                _listenerPortWasUnboundDuringValidation = false;
            }
            finally
            {
                listener.Stop();
            }
        }
        finally
        {
            policyProvider.Release();
        }

        _failure = await Catch.Exception(() => starting);
    }

    [Fact] void should_refuse_the_unknown_policy() => _failure.ShouldBeOfExactType<InvalidAuthorizationConfiguration>();
    [Fact] void should_not_bind_kestrel_before_validation_finishes() => _listenerPortWasUnboundDuringValidation.ShouldBeTrue();

    [Authorize(Policy = "PendingUnknown")]
    public record MissingPolicyCommand;

    public class DelayedUnknownPolicyProvider : IAuthorizationPolicyProvider
    {
        readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Entered => _entered.Task;

        public void Release() => _released.TrySetResult();

        public async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            _entered.TrySetResult();
            await _released.Task;
            return null;
        }

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync() =>
            Task.FromResult(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => Task.FromResult<AuthorizationPolicy?>(null);
    }
}
