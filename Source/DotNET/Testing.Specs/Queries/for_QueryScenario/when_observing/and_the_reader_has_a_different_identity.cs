// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Http;
using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.for_QueryScenario.when_observing;

public class and_the_reader_has_a_different_identity : given.an_observable_query
{
    CurrentPrincipalAccessor _principal = default!;
    QueryResult _result = default!;
    string? _restoredIdentity;

    void Establish()
    {
        var requests = Substitute.For<IHttpRequestContextAccessor>();
        requests.Current.Returns((IHttpRequestContext?)null);
        _principal = new(requests);
        _scenario.Services.AddSingleton<IAuthorizationPolicyRuntime>(new ChangingPrincipalRuntime());
        _scenario.Services.AddScoped<ObservableQueryScope>();
        var interceptor = Substitute.For<IInterceptReadModel<ObservableScenarioReadModel>>();
        interceptor.Intercept(Arg.Any<ObservableScenarioReadModel>()).Returns(_ => new ObservableScenarioReadModel(_principal.Current!.Identity!.AuthenticationType!));
        _scenario.Services.AddSingleton(interceptor);
    }

    async Task Because()
    {
        var observation = await _scenario.Observe(nameof(ObservableScenarioReadModel.Scoped));
        using var reader = _principal.BeginScope(new ClaimsPrincipal(new ClaimsIdentity([], "reader")));
        _state.Subject.OnNext(new("Changed"));
        _result = await observation.Next();
        _restoredIdentity = _principal.Current?.Identity?.AuthenticationType;
    }

    [Fact] void should_intercept_under_the_admitted_identity() => ((ObservableScenarioReadModel)_result.Data).Name.ShouldEqual("selected");
    [Fact] void should_restore_the_readers_identity() => _restoredIdentity.ShouldEqual("reader");
}
