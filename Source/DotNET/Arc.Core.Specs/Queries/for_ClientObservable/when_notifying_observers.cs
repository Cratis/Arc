// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ClientObservable;

public class when_notifying_observers : given.a_client_observable
{
    TestData _newData;
    TestData _receivedData;

    void Establish()
    {
        _newData = new TestData("Updated");
        _subject.Subscribe(data => _receivedData = data);
    }

    void Because() => _clientObservable.OnNext(_newData);

    [Fact] void should_notify_all_subscribers() => _receivedData.ShouldEqual(_newData);
}
