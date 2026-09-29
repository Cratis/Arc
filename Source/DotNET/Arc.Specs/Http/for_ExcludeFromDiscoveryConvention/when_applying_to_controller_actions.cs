// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Cratis.Arc.Http.for_ExcludeFromDiscoveryConvention;

public class when_applying_to_controller_actions : Specification
{
    ActionModel _marked;
    ActionModel _unmarked;
    ActionModel _classMarked;

    void Because()
    {
        var application = new ApplicationModel();
        var controller = new ControllerModel(typeof(ExampleController).GetTypeInfo(), []);
        _marked = new ActionModel(typeof(ExampleController).GetMethod(nameof(ExampleController.Hidden))!, []);
        _unmarked = new ActionModel(typeof(ExampleController).GetMethod(nameof(ExampleController.Visible))!, []);
        controller.Actions.Add(_marked);
        controller.Actions.Add(_unmarked);
        application.Controllers.Add(controller);

        var excludedController = new ControllerModel(typeof(ExcludedController).GetTypeInfo(), []);
        _classMarked = new ActionModel(typeof(ExcludedController).GetMethod(nameof(ExcludedController.Query))!, []);
        excludedController.Actions.Add(_classMarked);
        application.Controllers.Add(excludedController);

        new ExcludeFromDiscoveryConvention().Apply(application);
    }

    [Fact] void should_hide_a_marked_action() => _marked.ApiExplorer.IsVisible.ShouldEqual(false);
    [Fact] void should_keep_an_unmarked_action_visible() => _unmarked.ApiExplorer.IsVisible.ShouldBeNull();
    [Fact] void should_hide_actions_of_a_marked_controller() => _classMarked.ApiExplorer.IsVisible.ShouldEqual(false);

    public class ExampleController
    {
        [ExcludeFromDiscovery]
        public void Hidden() { }
        public void Visible() { }
    }

    [ExcludeFromDiscovery]
    public class ExcludedController
    {
        public void Query() { }
    }
}
