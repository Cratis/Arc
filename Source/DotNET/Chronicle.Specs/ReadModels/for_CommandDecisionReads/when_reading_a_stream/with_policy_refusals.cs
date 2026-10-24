// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads.when_reading_a_stream;

public class with_policy_refusals : given.a_stream_reader
{
    [Fact]
    public async Task should_refuse_get_on_an_unmarked_command()
    {
        Begin(typeof(UnmarkedCommand));
        var error = await Catch.Exception(() => _reader.Get<Model>("owner", "reports", "period"));
        error.ShouldBeOfExactType<ProtectedDecisionReadRequiresProtectedCommand>();
    }

    [Fact]
    public async Task should_refuse_detached_get_on_a_protected_command()
    {
        Begin(typeof(ProtectedCommand));
        var error = await Catch.Exception(() => _reader.GetDetached<Model>("owner", "reports", "period"));
        error.ShouldBeOfExactType<DecisionReadCouldNotBeAcquired>();
    }

    [Fact]
    public async Task should_refuse_a_missing_command_transaction()
    {
        Begin(typeof(ProtectedCommand));
        var error = await Catch.Exception(() => _reader.Get<Model>("owner", "reports", "period"));
        error.ShouldBeOfExactType<DecisionReadCouldNotBeAcquired>();
        await _inner.DidNotReceiveWithAnyArgs().GetDetached<Model>(default!, default!, default!, cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task should_refuse_a_custom_transaction_before_folding()
    {
        Begin(typeof(ProtectedCommand));
        CommandTransaction.Current = Substitute.For<IUnitOfWork>();
        var error = await Catch.Exception(() => _reader.Get<Model>("owner", "reports", "period"));
        error.ShouldBeOfExactType<DecisionReadCouldNotBeAcquired>();
        await _inner.DidNotReceiveWithAnyArgs().GetDetached<Model>(default!, default!, default!, cancellationToken: Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(typeof(UnprotectedCommand))]
    [InlineData(typeof(UnmarkedCommand))]
    public async Task should_allow_advisory_detached_reads(Type commandType)
    {
        Begin(commandType);
        var read = await _reader.GetDetached<Model>("owner", "reports", "period", cancellationToken: global::Xunit.TestContext.Current.CancellationToken);
        ReferenceEquals(read, _token).ShouldBeTrue();
        _unit.HasEnrolledDecisionReads.ShouldBeFalse();
    }

    [Fact]
    public void should_delegate_stream_admission() => _reader.AdmitForStream<Model>().ShouldEqual(_inner.Received(1).AdmitForStream<Model>());

    [Fact]
    public void should_wrap_protected_stream_admission_failures()
    {
        Begin(typeof(ProtectedCommand));
        _inner.AdmitForStream<Model>().Returns(_ => throw new DecisionReadRefused(DecisionReadRefusalReason.StreamScopeNotSupported, typeof(Model)));
        Catch.Exception(() => _reader.AdmitForStream<Model>()).ShouldBeOfExactType<DecisionReadCouldNotBeAcquired>();
    }
}
