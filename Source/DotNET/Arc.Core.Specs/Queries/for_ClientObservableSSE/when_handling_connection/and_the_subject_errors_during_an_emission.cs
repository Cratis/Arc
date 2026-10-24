// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Queries.for_ClientObservableSSE.when_handling_connection;

public class and_the_subject_errors_during_an_emission : given.a_guarded_client_observable_sse
{
    readonly TaskCompletionSource _sending = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly List<QueryResult> _results = [];
    bool _errorSentBeforeEmissionFinished;

    void Establish() => _context.Write(Arg.Any<string>(), Arg.Any<CancellationToken>())
        .Returns(async call =>
        {
            var result = JsonSerializer.Deserialize<QueryResult>(call.Arg<string>()["data: ".Length..].Trim(), _arcOptions.Value.JsonSerializerOptions);
            _results.Add(result!);
            if (!result!.HasExceptions)
            {
                _sending.TrySetResult();
                await _release.Task;
            }
        });

    async Task Because()
    {
        var connection = _observable.HandleConnection(_context);
        _subject.OnNext("value");
        await _sending.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _subject.OnError(new InvalidOperationException("The source failed"));
        _errorSentBeforeEmissionFinished = _results.Count > 1;
        _release.TrySetResult();
        await connection.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_wait_for_the_in_flight_emission() => _errorSentBeforeEmissionFinished.ShouldBeFalse();
    [Fact] void should_send_the_error_after_the_emission() => _results.Select(_ => _.HasExceptions).ShouldEqual([false, true]);
}
