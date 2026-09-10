// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Commands;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.given;

public class an_observed_transactional_scope : a_transactional_command_scope
{
    protected Subject<IEnumerable<AppendedEventWithResult>> _appends;
    protected CommandResult _result;

    void Establish()
    {
        _appends = new();
        var eventLog = Substitute.For<IEventLog>();
        eventLog.AppendOperations.Returns(_appends);
        _serviceProvider.GetService(typeof(IEventLog)).Returns(eventLog);
    }

    void Destroy() => _appends.Dispose();
}
