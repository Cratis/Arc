// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Http;
using Cratis.Arc.Tenancy;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries.for_ClientObservableSSE.when_streaming;

public class and_a_plain_context_emits_from_a_selected_foreign_producer
{
    [Fact]
    public async Task should_restore_the_subscriber_in_interceptors_and_guards()
    {
        var requestAccessor = new HttpRequestContextAccessor();
        var principalAccessor = new CurrentPrincipalAccessor(requestAccessor);
        var tenantResolver = Substitute.For<ITenantIdResolver>();
        tenantResolver.Resolve().Returns(_ => requestAccessor.Current?.Headers["X-Tenant"] ?? string.Empty);
        var tenants = new TenantIdAccessor(tenantResolver);
        await using var services = new ServiceCollection()
            .AddSingleton(requestAccessor)
            .AddSingleton(principalAccessor)
            .AddSingleton(tenants)
            .BuildServiceProvider();
        var subscriber = Principal("A");
        var producer = Principal("C");
        var context = Substitute.For<IHttpRequestContext>();
        context.RequestServices.Returns(services);
        context.User.Returns(subscriber);
        context.Headers.Returns(new Dictionary<string, string> { ["X-Tenant"] = "tenant-A" });
        context.RequestAborted.Returns(CancellationToken.None);
        context.Write(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var foreign = Substitute.For<IHttpRequestContext>();
        foreign.User.Returns(producer);
        foreign.RequestServices.Returns(services);
        foreign.Headers.Returns(new Dictionary<string, string> { ["X-Tenant"] = "tenant-C" });
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var observed = new TaskCompletionSource<(string? Principal, string Tenant, IHttpRequestContext? Context, string? GuardPrincipal, string GuardTenant)>(TaskCreationOptions.RunContinuationsAsynchronously);
        var intercepted = (Principal: (string?)null, Tenant: string.Empty, Context: (IHttpRequestContext?)null);
        var interceptors = Substitute.For<IReadModelInterceptors>();
        interceptors.Intercept(Arg.Any<Type>(), Arg.Any<IEnumerable<object>>(), Arg.Any<IServiceProvider>()).Returns(async call =>
        {
            await Task.Yield();
            intercepted = (principalAccessor.Current?.Identity?.Name, tenants.Current.Value, requestAccessor.Current);
            return call.ArgAt<IEnumerable<object>>(1);
        });
        var guards = Substitute.For<IObservableQueryEmissionGuards>();
        guards.HasGuards.Returns(true);
        guards.Guard(Arg.Any<ObservableQueryEmissionContext>()).Returns(async _ =>
        {
            started.TrySetResult();
            await release.Task;
            observed.TrySetResult((intercepted.Principal, intercepted.Tenant, intercepted.Context, principalAccessor.Current?.Identity?.Name, tenants.Current.Value));
            return ObservableQueryEmissionVerdict.Allow;
        });
        var lifetime = Substitute.For<IHostApplicationLifetime>();
        lifetime.ApplicationStopping.Returns(CancellationToken.None);
        var subject = new Subject<string>();
        var query = new QueryContext("Controller.Watch", CorrelationId.New(), Paging.NotPaged, Sorting.None);
        var observable = new ClientObservableSSE<string>(query, subject, interceptors, requestAccessor, Options.Create(new ArcOptions()), lifetime, guards, Substitute.For<ILogger<ClientObservableSSE<string>>>());
        requestAccessor.Current = context;
        var connection = observable.HandleConnection(context);
        var producerTask = Task.Run(async () =>
        {
            requestAccessor.Current = foreign;
            using (principalAccessor.UseAuthorizationPrincipal(producer, services))
            using (tenants.Begin("tenant-C"))
            {
                subject.OnNext("payload");
                await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            }
        });
        try
        {
            await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            subject.OnCompleted();
            connection.IsCompleted.ShouldBeFalse();
            release.TrySetResult();
            var emission = await observed.Task.WaitAsync(TimeSpan.FromSeconds(5));
            emission.Principal.ShouldEqual("A");
            emission.Tenant.ShouldEqual("tenant-A");
            emission.Context.ShouldEqual(context);
            emission.GuardPrincipal.ShouldEqual("A");
            emission.GuardTenant.ShouldEqual("tenant-A");
            await connection.WaitAsync(TimeSpan.FromSeconds(5));
            await producerTask;
        }
        finally
        {
            release.TrySetResult();
            requestAccessor.Current = null;
        }
    }

    static ClaimsPrincipal Principal(string name) => new(new ClaimsIdentity([new Claim(ClaimTypes.Name, name)], "test"));
}
