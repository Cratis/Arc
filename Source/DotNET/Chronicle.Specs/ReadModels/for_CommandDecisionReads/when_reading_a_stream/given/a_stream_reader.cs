// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads.when_reading_a_stream.given;

public class a_stream_reader : Specification
{
    protected IDecisionReads _inner;
    private protected CommandDecisionReads _reader;
    protected UnitOfWork _unit;
    protected DecisionRead<Model> _token;
    protected DecisionRead<Model> _read;
    protected IDisposable _policy;

    void Establish()
    {
        var (store, _, unit) = DecisionFixtures.Transaction();
        _unit = unit;
        _inner = Substitute.For<DecisionReaderForSpecs>();
        _token = DecisionFixtures.Protected<Model>("stream-key", new());
        _inner.GetDetached<Model>("owner", "reports", "period", null, Arg.Any<CancellationToken>()).Returns(_token);
        _reader = new(_inner, store, Substitute.For<IReadModels>());
    }

    protected void Begin(Type commandType)
    {
        _policy = DecisionPolicyForSpecs.Begin(commandType);
        CommandDecisionReads.Begin(commandType);
    }

    void Destroy()
    {
        CommandDecisionReads.End();
        _policy?.Dispose();
        CommandTransaction.Current = null;
    }

    public class Model;
    [ProtectedDecision]
    public class ProtectedCommand;
    [Unprotected]
    public class UnprotectedCommand;
    public class UnmarkedCommand;
}
