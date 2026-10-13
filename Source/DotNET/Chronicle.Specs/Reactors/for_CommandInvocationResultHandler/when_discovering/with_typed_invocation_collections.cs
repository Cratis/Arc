// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandInvocationResultHandler.when_discovering;

public class with_typed_invocation_collections : given.an_invocation_dispatcher
{
    bool[] _discovered;

    void Because() => _discovered = typeof(InvocationReactor).GetMethods()
        .Where(method => method.DeclaringType == typeof(InvocationReactor))
        .Select(method => method.IsEventHandlerMethod([typeof(RefusalRecorded)], _dispatcher)).ToArray();

    [Fact] void should_discover_all_synchronous_and_task_collection_signatures() => _discovered.ShouldEqual([true, true, true, true, true, true]);

    public class InvocationReactor : IReactor
    {
        public CommandInvocation[] Array(RefusalRecorded @event) => [Invoke.Command(new TestCommand(@event.Message))];
        public List<CommandInvocation> List(RefusalRecorded @event) => [Invoke.Command(new TestCommand(@event.Message))];
        public IEnumerable<CommandInvocation> Enumerable(RefusalRecorded @event) => [Invoke.Command(new TestCommand(@event.Message))];
        public Task<CommandInvocation[]> TaskArray(RefusalRecorded @event) => Task.FromResult(Array(@event));
        public Task<List<CommandInvocation>> TaskList(RefusalRecorded @event) => Task.FromResult(List(@event));
        public Task<IEnumerable<CommandInvocation>> TaskEnumerable(RefusalRecorded @event) => Task.FromResult(Enumerable(@event));
    }
}
