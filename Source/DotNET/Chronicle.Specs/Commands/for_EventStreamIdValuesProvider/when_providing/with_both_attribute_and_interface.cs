// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_EventStreamIdValuesProvider.when_providing;

public class with_both_attribute_and_interface : Specification
{
    EventStreamIdValuesProvider _provider;
    object _command;
    Exception _exception;

    void Establish()
    {
        _provider = new EventStreamIdValuesProvider();

        // Emit the invalid combination so the compiler analyzer cannot prevent the runtime rejection spec.
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("AmbiguousEventStreamIdFixture"), AssemblyBuilderAccess.Run);
        var commandType = assembly.DefineDynamicModule("Fixtures").DefineType("TestCommand", TypeAttributes.Public, typeof(TestCommand));
        var attributeConstructor = typeof(EventStreamIdAttribute).GetConstructor([typeof(string), typeof(bool)])!;
        commandType.SetCustomAttribute(new CustomAttributeBuilder(attributeConstructor, ["Monthly", false]));
        _command = Activator.CreateInstance(commandType.CreateType()!)!;
    }

    void Because() => _exception = Catch.Exception(() => _provider.Provide(_command));

    [Fact] void should_throw_ambiguous_event_stream_id() => _exception.ShouldBeOfExactType<AmbiguousEventStreamId>();
    [Fact] void should_indicate_command_type() => _exception.Message.ShouldContain("TestCommand");

    public class TestCommand : ICanProvideEventStreamId
    {
        public EventStreamId GetEventStreamId() => "Quarterly";
    }
}
