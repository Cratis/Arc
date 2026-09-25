// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Arc.Http;
using Cratis.Execution;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries.for_ClientObservableSSE.when_streaming;

public class and_the_subject_completes_with_queued_emissions
{
    [Fact]
    public async Task should_send_every_accepted_item_in_order_before_ending_the_request()
    {
        var subject = new Subject<int>();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var writes = new List<int>();
        var context = Substitute.For<IHttpRequestContext>();
        context.RequestServices.Returns(Substitute.For<IServiceProvider>());
        context.RequestAborted.Returns(CancellationToken.None);
        context.Write(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            using var json = JsonDocument.Parse(call.Arg<string>()["data: ".Length..]);
            writes.Add(json.RootElement.GetProperty("data").GetInt32());
            return Task.CompletedTask;
        });
        var interceptors = Substitute.For<IReadModelInterceptors>();
        interceptors.Intercept(Arg.Any<Type>(), Arg.Any<IEnumerable<object>>(), Arg.Any<IServiceProvider>())
            .Returns(call => Task.FromResult(call.ArgAt<IEnumerable<object>>(1)));
        var guards = Substitute.For<IObservableQueryEmissionGuards>();
        guards.HasGuards.Returns(true);
        guards.Guard(Arg.Any<ObservableQueryEmissionContext>()).Returns(async _ =>
        {
            entered.TrySetResult();
            await release.Task;
            return ObservableQueryEmissionVerdict.Allow;
        });
        var lifetime = Substitute.For<IHostApplicationLifetime>();
        lifetime.ApplicationStopping.Returns(CancellationToken.None);
        var observable = new ClientObservableSSE<int>(
            new QueryContext("Controller.Watch", CorrelationId.New(), Paging.NotPaged, Sorting.None),
            subject,
            interceptors,
            Substitute.For<IHttpRequestContextAccessor>(),
            Options.Create(new ArcOptions()),
            lifetime,
            guards,
            Substitute.For<ILogger<ClientObservableSSE<int>>>());
        var connection = observable.HandleConnection(context);
        subject.OnNext(1);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        subject.OnNext(2);
        subject.OnCompleted();
        try
        {
            connection.IsCompleted.ShouldBeFalse();
            release.TrySetResult();
            await connection.WaitAsync(TimeSpan.FromSeconds(5));
            writes.ShouldContainOnly([1, 2]);
            writes[0].ShouldEqual(1);
            writes[1].ShouldEqual(2);
        }
        finally
        {
            release.TrySetResult();
        }
    }
}
