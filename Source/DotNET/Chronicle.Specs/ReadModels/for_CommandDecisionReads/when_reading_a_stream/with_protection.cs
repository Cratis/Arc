// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;

namespace Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads.when_reading_a_stream;

public class with_protection : given.a_stream_reader
{
    Exception _providedError;

    async Task Because()
    {
        CommandTransaction.Current = _unit;
        Begin(typeof(ProtectedCommand));
        _read = await _reader.Get<Model>("owner", "reports", "period");
        _providedError = Catch.Exception(() => CommandDecisionReads.VerifyProvided(_read));
    }

    [Fact] void should_return_the_protected_token() => ReferenceEquals(_read, _token).ShouldBeTrue();
    [Fact] void should_enroll_in_the_command_transaction() => _unit.HasEnrolledDecisionReads.ShouldBeTrue();
    [Fact] void should_accept_the_token_from_provide() => _providedError.ShouldBeNull();
    [Fact] Task should_use_the_detached_stream_fold() => _inner.Received(1).GetDetached<Model>("owner", "reports", "period", null, Arg.Any<CancellationToken>());
    [Fact] Task should_not_use_the_ambient_enrollment() => _inner.DidNotReceive().Get<Model>("owner", "reports", "period", null, Arg.Any<CancellationToken>());
}
