// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandContextValuesBuilder.given;

public class a_command_context_values_builder : Specification
{
    protected CommandContextValuesBuilder _builder;
    protected IInstancesOf<ICommandContextValuesProvider> _providers;
    protected object _command;

    void Establish()
    {
        _providers = Substitute.For<IInstancesOf<ICommandContextValuesProvider>>();
        _builder = new CommandContextValuesBuilder(_providers);
        _command = new { Name = "TestCommand", Value = 42 };
    }
}