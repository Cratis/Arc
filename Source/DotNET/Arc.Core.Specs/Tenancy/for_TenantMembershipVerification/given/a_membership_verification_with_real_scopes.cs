// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Http;
using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Tenancy.for_TenantMembershipVerification.given;

public class a_membership_verification_with_real_scopes : Specification
{
    protected ConcurrentQueue<RecordingMembership> _created;
    protected HttpRequestContextAccessor _requests;
    internal ILogger<TenantMembershipVerification> _logger;
    internal TenantMembershipVerification _verification;
    protected ServiceProvider _root;
    protected List<IDisposable> _disposables;

    void Establish()
    {
        _created = new();
        _disposables = [];
        _requests = new HttpRequestContextAccessor();
        _logger = Substitute.For<ILogger<TenantMembershipVerification>>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _verification = new TenantMembershipVerification(_requests, new CurrentPrincipalAccessor(_requests), _logger);
    }

    void Destroy()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }

        _root?.Dispose();
    }

    protected static ClaimsPrincipal Principal(string name) => new(new ClaimsIdentity([new Claim(ClaimTypes.Name, name)], "test"));

    /// <summary>Builds a root provider where each resolution of the hook creates and records a new instance.</summary>
    /// <param name="lifetime">The hook lifetime.</param>
    protected void BuildRoot(ServiceLifetime lifetime)
    {
        var services = new ServiceCollection();
        object Create(IServiceProvider provider)
        {
            var membership = new RecordingMembership();
            _created.Enqueue(membership);
            return membership;
        }

        services.Add(new ServiceDescriptor(typeof(ITenantMembership), Create, lifetime));
        _root = services.BuildServiceProvider();
    }

    protected IServiceScope NewScope()
    {
        var scope = _root.CreateScope();
        _disposables.Add(scope);
        return scope;
    }

    /// <summary>Creates a subscribe request carrying a real, shared items dictionary.</summary>
    /// <param name="user">The request user.</param>
    /// <returns>The request.</returns>
    protected static IHttpRequestContext Request(ClaimsPrincipal user)
    {
        var request = Substitute.For<IHttpRequestContext>();
        request.User.Returns(user);
        request.Items.Returns(new Dictionary<object, object?>());
        request.Query.Returns(new Dictionary<string, string>());
        request.Headers.Returns(new Dictionary<string, string>());
        request.Cookies.Returns(new Dictionary<string, string>());
        return request;
    }

    internal static ObservableQuerySubscriptionHttpRequestContext Subscription(IHttpRequestContext request, IServiceProvider services) =>
        new(request, request, services, CancellationToken.None);

    internal static ObservableQuerySubscriptionHttpRequestContext Subscription(IServiceProvider services, string user) =>
        Subscription(Request(Principal(user)), services);

    /// <summary>Runs the verification of two contexts on two flows, interleaved check by check.</summary>
    /// <param name="first">The first context.</param>
    /// <param name="firstTenant">The first context's tenant.</param>
    /// <param name="second">The second context.</param>
    /// <param name="secondTenant">The second context's tenant.</param>
    /// <param name="checks">The number of checks per flow.</param>
    internal void VerifyInterleaved(IHttpRequestContext first, TenantId firstTenant, IHttpRequestContext second, TenantId secondTenant, int checks)
    {
        using var barrier = new Barrier(2);
        Task Flow(IHttpRequestContext context, TenantId tenant) => Task.Run(() =>
        {
            _requests.Current = context;
            for (var index = 0; index < checks; index++)
            {
                barrier.SignalAndWait(TimeSpan.FromSeconds(30));
                _verification.Verify(tenant);
            }
        });

        Task.WaitAll(Flow(first, firstTenant), Flow(second, secondTenant));
    }
}
