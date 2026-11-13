// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Authorization;
using Cratis.Arc.Http;
using Cratis.Arc.Queries.given;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ObservableQueryHttp.when_intercepting_an_emission;

public class and_a_verified_caller_is_admitted : given.a_request
{
    protected emission_principals _principals;
    protected virtual bool Anonymous => false;
    protected virtual bool UsesObservable => false;

    void Establish()
    {
        _principals = new(Anonymous);
        _services = new ServiceCollection()
            .AddSingleton(_interceptors)
            .AddSingleton(_guards)
            .AddSingleton<IHttpRequestContextAccessor>(_principals.Requests)
            .AddSingleton<CurrentPrincipalAccessor>()
            .BuildServiceProvider();
        _request.RequestServices.Returns(_services);
        _principals.Attach(_request);
        _guards.Guard(Arg.Any<ObservableQueryEmissionContext>()).Returns(call => Task.FromResult(_principals.Evaluate(call.Arg<ObservableQueryEmissionContext>())));
    }

    async Task Because()
    {
        _response = UsesObservable
            ? await Observe()
            : await ObservableQueryHttp.CreateResponse(_queryContext, Items(), new(false, TimeSpan.FromSeconds(1)), _request);
    }

    async Task<ObservableQueryHttpResponse> Observe()
    {
        using var subject = new Subject<string>();
        var response = ObservableQueryHttp.CreateResponse(_queryContext, subject, new(true, TimeSpan.FromSeconds(3)), _request);
        _principals.ChangeRequestPrincipal();
        subject.OnNext("first");
        await _principals.FirstGuard.WaitAsync(TimeSpan.FromSeconds(3));
        subject.OnNext("second");
        await _principals.BothGuards.WaitAsync(TimeSpan.FromSeconds(3));
        subject.OnCompleted();
        return await response;
    }

    async IAsyncEnumerable<string> Items()
    {
        _principals.ChangeRequestPrincipal();
        yield return "first";
        await Task.Yield();
        yield return "second";
    }

    [Fact] void should_keep_the_admitted_subject() => _principals.ShouldKeepTheAdmittedSubject();
    [Fact] void should_keep_the_ambient_subject() => _principals.ShouldKeepTheAmbientSubject();
    [Fact] void should_not_gain_host_privileges() => _principals.ShouldNotGainPrivilege();
    [Fact] void should_not_upgrade_authentication() => _principals.ShouldNotUpgradeAuthentication();
    [Fact] void should_evaluate_both_emissions() => _principals.ShouldEvaluateBothEmissions();
    [Fact] void should_withhold_both_values() => (_response.Result.Data as IEnumerable<string> ?? []).ShouldBeEmpty();
}
