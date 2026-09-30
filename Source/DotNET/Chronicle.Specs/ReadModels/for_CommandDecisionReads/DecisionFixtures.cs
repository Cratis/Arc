// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads;

internal static class DecisionFixtures
{
    public static readonly EventStoreName StoreName = (EventStoreName)"store";
    public static readonly EventStoreNamespaceName NamespaceName = (EventStoreNamespaceName)"namespace";

    /// <summary>
    /// Creates a package-issued token for focused Arc tests without a live projection kernel.
    /// The scope itself remains opaque to application code.
    /// </summary>
    /// <typeparam name="T">The test model.</typeparam>
    /// <param name="key">The source key.</param>
    /// <param name="instance">The optional folded model.</param>
    /// <returns>A protected decision read issued by the Chronicle package type.</returns>
    public static DecisionRead<T> Protected<T>(string key, T? instance = null)
        where T : class =>
        (DecisionRead<T>)typeof(DecisionRead<T>).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(_ => _.GetParameters().Length == 6)
            .Invoke([(ReadModelKey)key, instance, StoreName, NamespaceName, (EventSequenceNumber)1ul,
                new[] { new EventType((EventTypeId)Guid.NewGuid().ToString(), EventTypeGeneration.First) }]);

    public static (IEventStore Store, IEventLog Log, UnitOfWork Unit) Transaction()
    {
        var store = Substitute.For<IEventStore>();
        var log = Substitute.For<IEventLog>();
        store.Name.Returns(StoreName);
        store.Namespace.Returns(NamespaceName);
        store.GetEventSequence(EventSequenceId.Log).Returns(log);
        var unit = new UnitOfWork(CorrelationId.New(), _ => { }, store);
        return (store, log, unit);
    }
}
