// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Traces;

namespace Cratis.Arc.Commands.for_CommandActionFilter.given;

public class a_command_action_filter : Specification
{
    protected ICommandContextModifier _contextModifier;
    protected ICommandContextValuesBuilder _contextValuesBuilder;
    protected CommandContextValues _builtValues;
    protected CommandActionFilter _filter;
    System.Diagnostics.ActivitySource _activitySource;

    void Establish()
    {
        _contextModifier = Substitute.For<ICommandContextModifier>();
        _contextValuesBuilder = Substitute.For<ICommandContextValuesBuilder>();
        _builtValues = new CommandContextValues { { "key", "value" } };
        _contextValuesBuilder.Build(Arg.Any<object>()).Returns(_builtValues);
        _filter = new CommandActionFilter(_contextModifier, _contextValuesBuilder, CreateActivitySource<CommandActionFilter>());
    }

    void Cleanup()
    {
        _activitySource?.Dispose();
    }

    IActivitySource<T> CreateActivitySource<T>()
    {
        var activitySource = Substitute.For<IActivitySource<T>>();
        _activitySource = new System.Diagnostics.ActivitySource("Cratis.Arc.Test");
        activitySource.ActualSource.Returns(_activitySource);
        return activitySource;
    }
}
