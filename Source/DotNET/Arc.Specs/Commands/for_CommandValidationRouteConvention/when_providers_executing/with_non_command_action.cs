// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Cratis.Arc.Commands.for_CommandValidationRouteConvention.when_providers_executing;

public class with_non_command_action : given.a_command_validation_route_convention
{
    ControllerModel _controller;
    ActionModel _action;
    ApplicationModel _applicationModel;

    void Establish()
    {
        _applicationModel = new ApplicationModel();
        _controller = new ControllerModel(typeof(TestController).GetTypeInfo(), []);
        var method = typeof(TestController).GetMethod(nameof(TestController.Query));
        _action = new ActionModel(method, [new HttpGetAttribute()]);

        var selector = new SelectorModel
        {
            AttributeRouteModel = new AttributeRouteModel
            {
                Template = "api/query"
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

    [Fact] void should_not_add_validation_selector() => _action.Selectors.Count.ShouldEqual(1);

    class TestController : ControllerBase
    {
        [HttpGet]
        public void Query()
        {
        }
    }
}
