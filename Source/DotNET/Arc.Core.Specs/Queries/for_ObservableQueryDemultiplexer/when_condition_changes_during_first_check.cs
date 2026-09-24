// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer;

public class when_condition_changes_during_first_check : given.an_observable_query_demultiplexer
{
    bool _condition;
    int _checks;

    async Task Because() => await WaitFor(CheckCondition);

    [Fact] void should_observe_the_pulse_without_waiting_for_another_change() => _checks.ShouldEqual(2);

    bool CheckCondition()
    {
        _checks++;
        if (_checks == 1)
        {
            _condition = true;
            _signals.Signal();
            return false;
        }

        return _condition;
    }
}
