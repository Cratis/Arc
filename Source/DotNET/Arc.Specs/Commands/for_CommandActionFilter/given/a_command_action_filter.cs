// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandActionFilter.given;

public class a_command_action_filter : Specification
{
    protected ICommandContextModifier _contextModifier;
    protected ICommandContextValuesBuilder _contextValuesBuilder;
    protected CommandContextValues _builtValues;
    protected CommandActionFilter _filter;

    void Establish()
    {
        _contextModifier = Substitute.For<ICommandContextModifier>();
        _contextValuesBuilder = Substitute.For<ICommandContextValuesBuilder>();
        _builtValues = new CommandContextValues { { "key", "value" } };
        _contextValuesBuilder.Build(Arg.Any<object>()).Returns(_builtValues);
        _filter = new CommandActionFilter(_contextModifier, _contextValuesBuilder);
    }
}
