// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Cratis.Execution;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ControllerObservableQueryAdapter.given;

public class a_request_with_emission_processing : a_controller_observable_query_adapter
{
    protected ServiceProvider _services;
    protected ObjectResult _result;
    protected IReadModelInterceptors _interceptors;
    protected IObservableQueryEmissionGuards _guards;

    void Establish()
    {
        _interceptors = Substitute.For<IReadModelInterceptors>();
        _interceptors.Intercept(Arg.Any<Type>(), Arg.Any<IEnumerable<object>>(), Arg.Any<IServiceProvider>())
            .Returns(call => Task.FromResult(call.Arg<IEnumerable<object>>().Select(value => (object)((TestData)value with { Value = "intercepted" }))));
        _guards = Substitute.For<IObservableQueryEmissionGuards>();
        _guards.HasGuards.Returns(true);
        _guards.Guard(Arg.Any<ObservableQueryEmissionContext>()).Returns(Task.FromResult(ObservableQueryEmissionVerdict.Allow));
        _services = new ServiceCollection().AddSingleton(_interceptors).AddSingleton(_guards)
            .AddSingleton<IHttpRequestContextAccessor, HttpRequestContextAccessor>().BuildServiceProvider();
        _httpContext.RequestServices = _services;
        _queryContextManager.Current.Returns(new QueryContext("Controller.Observe", CorrelationId.New(), Paging.NotPaged, Sorting.None));
    }

    void Destroy() => _services.Dispose();

    protected async Task Handle(object data)
    {
        _result = new ObjectResult(data);
        var executed = new ActionExecutedContext(_actionContext, [], null!) { Result = _result };
        await _adapter.HandleControllerStreamingResult(_actionContext, executed, _result);
    }

    protected static async IAsyncEnumerable<TestData> TwoItems()
    {
        await Task.Yield();
        yield return new("first");
        yield return new("second");
    }
}
