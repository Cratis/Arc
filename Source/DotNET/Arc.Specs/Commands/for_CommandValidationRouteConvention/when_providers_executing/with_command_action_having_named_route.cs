// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Cratis.Arc.Commands.for_CommandValidationRouteConvention.when_providers_executing;

public class with_command_action_having_named_route : given.a_command_validation_route_convention
{
    const string RouteTemplate = "api/commands/execute";
    const string RouteName = "ExecuteCommand";
    ControllerModel _controller;
    ActionModel _action;
    ApplicationModel _applicationModel;

    void Establish()
    {
        _applicationModel = new ApplicationModel();
        _controller = new ControllerModel(typeof(TestController).GetTypeInfo(), []);
        var method = typeof(TestController).GetMethod(nameof(TestController.Execute));
        _action = new ActionModel(method, [new HttpPostAttribute()]);

        var selector = new SelectorModel
        {
            AttributeRouteModel = new AttributeRouteModel
            {
                Template = RouteTemplate,
                Name = RouteName
            }
        };
        _action.Selectors.Add(selector);

        _controller.Actions.Add(_action);
        _applicationModel.Controllers.Add(_controller);
    }

    void Because()
    {
        var context = new given.TestApplicationModelProviderContext(_applicationModel);
        _convention.OnProvidersExecuting(context);
    }

    [Fact] void should_add_validation_selector() => _action.Selectors.Count.ShouldEqual(2);
    [Fact] void should_add_validate_suffix_to_route_name() => _action.Selectors[1].AttributeRouteModel.Name.ShouldEqual($"{RouteName}Validate");

    class TestController : ControllerBase
    {
        [HttpPost]
        public void Execute()
        {
        }
    }
}
