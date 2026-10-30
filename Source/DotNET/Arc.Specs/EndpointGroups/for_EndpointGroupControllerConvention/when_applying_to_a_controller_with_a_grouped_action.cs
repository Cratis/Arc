// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Cratis.Arc.AspNetCore.EndpointGroups.for_EndpointGroupControllerConvention;

public class when_applying_to_a_controller_with_a_grouped_action : Specification
{
    ActionModel _groupedAction;
    ActionModel _publicAction;
    ActionModel _overriddenAction;

    void Because()
    {
        var application = new ApplicationModel();
        var controller = new ControllerModel(typeof(MixedController).GetTypeInfo(), []);
        _groupedAction = ActionFor(controller, nameof(MixedController.Internal));
        _publicAction = ActionFor(controller, nameof(MixedController.Public));
        application.Controllers.Add(controller);
        var grouped = new ControllerModel(typeof(GroupedController).GetTypeInfo(), []);
        _overriddenAction = ActionFor(grouped, nameof(GroupedController.Query));
        application.Controllers.Add(grouped);
        new EndpointGroupControllerConvention().Apply(application);
    }

    [Fact] void should_group_the_tagged_action() => GroupOf(_groupedAction).ShouldEqual("internal");
    [Fact] void should_hide_the_tagged_action() => _groupedAction.ApiExplorer.IsVisible.ShouldEqual(false);
    [Fact] void should_keep_the_sibling_public() => GroupOf(_publicAction).ShouldBeNull();
    [Fact] void should_keep_the_sibling_visible() => _publicAction.ApiExplorer.IsVisible.ShouldBeNull();
    [Fact] void should_prefer_the_action_group_over_the_controller_group() => GroupOf(_overriddenAction).ShouldEqual("ops");

    static string? GroupOf(ActionModel action) => action.Selectors[0].EndpointMetadata.OfType<EndpointGroupMetadata>().FirstOrDefault()?.Name;

    static ActionModel ActionFor(ControllerModel controller, string name)
    {
        var action = new ActionModel(controller.ControllerType.GetMethod(name)!, []);
        action.Selectors.Add(new SelectorModel());
        controller.Actions.Add(action);
        return action;
    }

    public class MixedController
    {
        [EndpointGroup("internal")]
        public void Internal() { }

        public void Public() { }
    }

    [EndpointGroup("internal")]
    public class GroupedController
    {
        [EndpointGroup("ops")]
        public void Query() { }
    }
}
