// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Contexts.for_ContextValues;

public class when_a_legacy_source_reads_an_execution : Specification
{
    IContextValueSource _source;
    ServiceProvider _services;
    object _value;

    void Establish()
    {
        _source = Substitute.For<IContextValueSource>();
        _source.Read(typeof(string), "value", Arg.Any<CancellationToken>()).Returns(ContextValueRead.Present("legacy"));
        _services = new ServiceCollection().AddSingleton(_source).BuildServiceProvider();
    }

    async Task Because()
    {
        var context = new CommandContext(CorrelationId.New(), typeof(string), "bound", [], new CommandContextValues(), ServiceProvider: _services);
        _value = await context.ProvidedValues.GetAsync("value");
    }

    void Destroy() => _services.Dispose();

    [Fact] void should_keep_the_old_source_callable() => _value.ShouldEqual("legacy");
}
