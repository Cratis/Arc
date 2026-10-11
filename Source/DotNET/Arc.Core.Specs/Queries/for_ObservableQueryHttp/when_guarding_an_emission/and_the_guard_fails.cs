// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Reactive.Subjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries.for_ObservableQueryHttp.when_guarding_an_emission;

public class and_the_guard_fails : given.a_request
{
    ServiceProvider _guardServices;

    void Establish()
    {
        var types = Substitute.For<ITypes>();
        types.FindMultiple<IGuardObservableQueryEmission>().Returns([typeof(FailingGuard)]);
        var guards = new ObservableQueryEmissionGuards(types, Substitute.For<ILogger<ObservableQueryEmissionGuards>>());
        _guardServices = new ServiceCollection().AddSingleton(_interceptors).AddSingleton(_accessor)
            .AddSingleton<IObservableQueryEmissionGuards>(guards).BuildServiceProvider();
        _request.RequestServices.Returns(_guardServices);
    }

    async Task Because()
    {
        using var subject = new BehaviorSubject<string>("private");
        _response = await ObservableQueryHttp.CreateResponse(_queryContext, subject, new(false, TimeSpan.FromSeconds(1)), _request);
    }

    void Destroy() => _guardServices.Dispose();

    [Fact] void should_return_forbidden() => _response.StatusCode.ShouldEqual(HttpStatusCode.Forbidden);
    [Fact] void should_not_return_data() => _response.Result.Data.ShouldBeNull();

    public class FailingGuard : IGuardObservableQueryEmission
    {
        public Task<ObservableQueryEmissionVerdict> Guard(ObservableQueryEmissionContext context) => Task.FromException<ObservableQueryEmissionVerdict>(new GuardFailed());
    }

    class GuardFailed : Exception;
}
