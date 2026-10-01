// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Arc.Commands.for_CommandProvideInvoker.when_invoking;

/// <summary>
/// A caller giving up is not a fault of the system, so it records the exception and leaves the span alone.
/// </summary>
public class and_it_is_traced_and_the_caller_cancels : given.a_traced_command_provide_invoker
{
    public class RegisterAuthor
    {
        public Task<string> Provide(CancellationToken cancellationToken) => Task.FromCanceled<string>(cancellationToken);
    }

    CancellationTokenSource _cancellation;
    Exception? _error;

    void Establish()
    {
        _cancellation = new();
        _cancellation.Cancel();
    }

    void Destroy() => _cancellation.Dispose();

    async Task Because() => _error = await Catch.Exception(async () => await Invoke(new RegisterAuthor(), _cancellation.Token));

    [Fact] void should_let_the_cancellation_through() => (_error is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_leave_the_status_unset() => ProvideSpan.Status.ShouldEqual(ActivityStatusCode.Unset);
    [Fact] void should_record_the_exception_type() => ProvideSpan.Events.Single(_ => _.Name == WellKnownTelemetryNames.ExceptionEvent).Tags.Single().Value.ShouldEqual(typeof(TaskCanceledException).FullName);
}
