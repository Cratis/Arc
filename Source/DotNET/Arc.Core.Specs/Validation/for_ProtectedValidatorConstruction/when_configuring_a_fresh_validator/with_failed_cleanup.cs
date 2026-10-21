// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Validation.for_ProtectedValidatorConstruction.when_configuring_a_fresh_validator;

public class with_failed_cleanup : Specification
{
    readonly List<string> _events = [];
    ProtectedValidatorActivationFrame _outer;
    ProtectedValidatorActivationFrame _inner;
    Exception _error;
    bool _restored;

    async Task Because()
    {
        _outer = new();
        _inner = new();
        _inner.Own(new Owned(_events, "first", false));
        _inner.Own(new Owned(_events, "second", true));
        _inner.Own(new Owned(_events, "last", true));
        _error = await Catch.Exception(() => _inner.DisposeAsync().AsTask());
        _restored = ReferenceEquals(ProtectedValidatorActivationFrame.Current, _outer);
    }

    async Task Destroy() => await _outer.DisposeAsync();

    [Fact] void should_fail_before_a_command_can_report_success() => _error.ShouldBeOfExactType<DiscoverableValidatorRefusedInProtectedDecision>();
    [Fact] void should_prefer_async_cleanup_and_attempt_every_object_in_reverse_order() => _events.ShouldEqual(["last", "second", "first"]);
    [Fact] void should_retain_all_cleanup_failures() => ((AggregateException)_error.InnerException).InnerExceptions.Count.ShouldEqual(2);
    [Fact] void should_restore_the_outer_frame_even_after_cleanup_failed() => _restored.ShouldBeTrue();

    sealed class Owned(List<string> events, string name, bool fails) : IDisposable, IAsyncDisposable
    {
        public void Dispose() => events.Add("wrong synchronous cleanup");
        public async ValueTask DisposeAsync()
        {
            await Task.Yield();
            events.Add(name);
            if (fails) throw new InvalidOperationException(name);
        }
    }
}
