// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;

namespace Cratis.Arc.Chronicle.Reactors.for_EventsAndCommandsResultHandler.when_discovering;

public class with_mixed_collection_handlers : given.a_mixed_result_handler
{
    bool[] _discovered;

    void Because() => _discovered = typeof(CollectionReactor).GetMethods().Where(method => method.DeclaringType == typeof(CollectionReactor)).Select(method => method.IsEventHandlerMethod([typeof(TestEvent)], _dispatcher)).ToArray();

    [Fact] void should_discover_sync_and_task_collection_signatures() => _discovered.ShouldContainOnly(true, true, true, true);

    public class CollectionReactor : IReactor
    {
        public IEnumerable<object> Enumerable(TestEvent @event) => [@event, new TestCommand("next")];
        public object[] Array(TestEvent @event) => [@event, new TestCommand("next")];
        public List<object> List(TestEvent @event) => [@event, new TestCommand("next")];
        public Task<IEnumerable<object>> Async(TestEvent @event) => Task.FromResult<IEnumerable<object>>([@event, new TestCommand("next")]);
    }
}
