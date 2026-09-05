// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions.when_injecting_read_model_into_provide;

public class without_read_model : given.nullable_read_model_injection
{
    class Command
    {
        public bool Provide(TestReadModel? readModel) => readModel is null;
    }

    CommandProvideInvoker _invoker;
    IReadOnlyList<object> _result;

    void Establish() => _invoker = new();

    async Task Because()
    {
        var command = new Command();
        var context = new CommandContext(CorrelationId.New(), typeof(Command), command, [], new());
        _result = await _invoker.Invoke(context, _serviceProvider);
    }

    [Fact] void should_inject_null() => _result[0].ShouldEqual(true);
}
