// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Http;
using Cratis.Execution;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries.for_ClientObservableSSE.when_streaming;

public class and_concurrent_emissions_and_disposal_occur
{
    [Fact]
    public async Task should_not_throw_when_next_and_complete_race()
    {
        // Arrange
        var subject = new Subject<int>();
        var httpContext = Substitute.For<IHttpRequestContext>();
        var arcOptions = Substitute.For<IOptions<ArcOptions>>();
        var options = new ArcOptions();
        arcOptions.Value.Returns(options);

        var hostLifetime = Substitute.For<IHostApplicationLifetime>();
        hostLifetime.ApplicationStopping.Returns(CancellationToken.None);

        var logger = Substitute.For<ILogger<ClientObservableSSE<int>>>();

        var writeTaskSource = new TaskCompletionSource();
        httpContext.Write(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(writeTaskSource.Task);

        var queryContext = new QueryContext(
            new FullyQualifiedQueryName("[TestApp].[TestQuery]"),
            CorrelationId.New(),
            new Paging(0, 10, true),
            Sorting.None);

        var observable = new ClientObservableSSE<int>(
            queryContext,
            subject,
            arcOptions,
            hostLifetime,
            logger);

        // Act & Assert: Rapid emissions followed by disposal should not throw
        var connectionTask = Task.Run(() => observable.HandleConnection(httpContext));

        // Emit several items rapidly
        for (var i = 0; i < 5; i++)
        {
            subject.OnNext(i);
            writeTaskSource.SetResult();
            writeTaskSource = new TaskCompletionSource();
            await Task.Yield();
        }

        // Complete the subject
        subject.OnCompleted();

        // Connection should complete within a reasonable time
        var timeout = Task.Delay(2000);
        var completed = await Task.WhenAny(connectionTask, timeout);

        if (completed == timeout)
        {
            throw new TimeoutException("Connection did not complete in time");
        }

        // Should not throw
        await connectionTask;
    }

    [Fact]
    public async Task should_handle_disposal_before_all_writes_complete()
    {
        // Arrange
        var subject = new Subject<int>();
        var httpContext = Substitute.For<IHttpRequestContext>();
        var arcOptions = Substitute.For<IOptions<ArcOptions>>();
        var options = new ArcOptions();
        arcOptions.Value.Returns(options);

        var hostLifetime = Substitute.For<IHostApplicationLifetime>();
        hostLifetime.ApplicationStopping.Returns(CancellationToken.None);

        var logger = Substitute.For<ILogger<ClientObservableSSE<int>>>();

        var writeTaskSource = new TaskCompletionSource();
        httpContext.Write(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(writeTaskSource.Task);

        var queryContext = new QueryContext(
            new FullyQualifiedQueryName("[TestApp].[TestQuery]"),
            CorrelationId.New(),
            new Paging(0, 10, true),
            Sorting.None);

        var observable = new ClientObservableSSE<int>(
            queryContext,
            subject,
            arcOptions,
            hostLifetime,
            logger);

        // Act: Start connection
        var connectionTask = Task.Run(() => observable.HandleConnection(httpContext));

        // Emit an item but don't complete the write
        subject.OnNext(42);
        await Task.Delay(10);

        // Complete before the write completes
        subject.OnCompleted();

        // Eventually complete the write
        await Task.Delay(50);
        writeTaskSource.SetResult();

        // Connection should complete without hanging
        var timeout = Task.Delay(2000);
        var completed = await Task.WhenAny(connectionTask, timeout);

        if (completed == timeout)
        {
            throw new TimeoutException("Connection did not complete");
        }

        // Should not throw
        await connectionTask;
    }
}
