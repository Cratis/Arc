// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Authorization.given;
using Cratis.Arc.Http;
using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Queries.given;

public class emission_principals(bool anonymous)
{
    readonly ClaimsPrincipal _admitted = anonymous
        ? new(new ClaimsIdentity())
        : VerifiedCallerPrincipal.Create(verified_callers.Person("admitted", "read"));
    readonly ClaimsPrincipal _host = VerifiedCallerPrincipal.Create(verified_callers.Person("host", "admin"));
    readonly HttpRequestContextAccessor _requests = new();
    readonly ConcurrentQueue<ClaimsPrincipal?> _ambient = [];
    readonly ConcurrentQueue<ClaimsPrincipal?> _guards = [];
    readonly List<IHttpRequestContext> _contexts = [];
    readonly TaskCompletionSource _firstGuard = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _bothGuards = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public IHttpRequestContextAccessor Requests => _requests;
    public Task FirstGuard => _firstGuard.Task;
    public Task BothGuards => _bothGuards.Task;

    public void Attach(IHttpRequestContext context)
    {
        _contexts.Add(context);
        context.Items.Returns(new Dictionary<object, object?> { [VerifiedCallerPrincipal.ItemKey] = _admitted });
        context.User.Returns(_host);
        var fallback = context.RequestServices;
        var services = Substitute.For<IServiceProvider>();
        var principals = new CurrentPrincipalAccessor(_requests);
        services.GetService(Arg.Any<Type>()).Returns(call => call.Arg<Type>() switch
        {
            var type when type == typeof(IHttpRequestContextAccessor) => _requests,
            var type when type == typeof(CurrentPrincipalAccessor) => principals,
            _ => fallback.GetService(call.Arg<Type>())
        });
        context.RequestServices.Returns(services);
    }

    public void ChangeRequestPrincipal()
    {
        ((ClaimsIdentity)_admitted.Identity!).AddClaim(new Claim(VerifiedCallerIdentity.ScopeClaim, "admin"));
        foreach (var context in _contexts)
        {
            context.Items[VerifiedCallerPrincipal.ItemKey] = _host;
            context.User = _host;
        }
    }

    public ObservableQueryEmissionVerdict Evaluate(ObservableQueryEmissionContext context)
    {
        _guards.Enqueue(context.Principal);
        _firstGuard.TrySetResult();
        if (_guards.Count == 2)
        {
            _bothGuards.TrySetResult();
        }
        _ambient.Enqueue(new CurrentPrincipalAccessor(_requests).Current);
        ChangeRequestPrincipal();
        return context.Principal?.HasClaim(VerifiedCallerIdentity.ScopeClaim, "admin") == true
            ? ObservableQueryEmissionVerdict.Allow
            : ObservableQueryEmissionVerdict.Suppress;
    }

    public void ShouldKeepTheAdmittedSubject() => _guards.Select(_ => _?.FindFirst(VerifiedCallerIdentity.SubjectClaim)?.Value).ShouldEachConformTo(_ => _ == (anonymous ? null : "admitted"));
    public void ShouldKeepTheAmbientSubject() => _ambient.Select(_ => _?.FindFirst(VerifiedCallerIdentity.SubjectClaim)?.Value).ShouldEachConformTo(_ => _ == (anonymous ? null : "admitted"));
    public void ShouldNotGainPrivilege() => _guards.Any(_ => _?.HasClaim(VerifiedCallerIdentity.ScopeClaim, "admin") == true).ShouldBeFalse();
    public void ShouldNotUpgradeAuthentication() => _guards.Select(_ => _?.Identity?.IsAuthenticated).ShouldEachConformTo(_ => _ == !anonymous);
    public void ShouldEvaluateBothEmissions() => _guards.Count.ShouldEqual(2);
}
