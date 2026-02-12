// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Cratis.Arc.Commands.for_CommandValidationRouteConvention.when_providers_executing;

public class with_command_action_having_multiple_selectors : given.a_command_validation_route_convention
{
    const string FirstRouteTemplate = "api/commands/execute";
    const string SecondRouteTemplate = "api/v2/commands/execute";
    ControllerModel _controller;
    ActionModel _action;
    ApplicationModel _applicationModel;

    void Establish()
    {
        _applicationModel = new ApplicationModel();
        _controller = new ControllerModel(typeof(TestController).GetTypeInfo(), []);
        var method = typeof(TestController).GetMethod(nameof(TestController.Execute));
        _action = new ActionModel(method, [new HttpPostAttribute()]);

        var firstSelector = new SelectorModel
        {
            AttributeRouteModel = new AttributeRouteModel
            {
                Template = FirstRouteTemplate
            }
        };
        _action.Selectors.Add(firstSelector);

        var secondSelector = new SelectorModel
        {
            AttributeRouteModel = new AttributeRouteModel
            {
                Template = SecondRouteTemplate
            }
        };
        _action.Selectors.Add(secondSelector);

        _controller.Actions.Add(_action);
        _applicationModel.Controllers.Add(_controller);
    }

    void Because()
    {
        var context = new given.TestApplicationModelProviderContext(_applicationModel);
        _convention.OnProvidersExecuting(context);
    }

    [Fact] void should_add_validation_selectors_for_each_original_selector() => _action.Selectors.Count.ShouldEqual(4);
    [Fact] void should_add_validate_suffix_to_first_template() => _action.Selectors.Any(_ => _.AttributeRouteModel.Template == $"{FirstRouteTemplate}/validate").ShouldBeTrue();
    [Fact] void should_add_validate_suffix_to_second_template() => _action.Selectors.Any(_ => _.AttributeRouteModel.Template == $"{SecondRouteTemplate}/validate").ShouldBeTrue();

    class TestController : ControllerBase
    {
        [HttpPost]
        public void Execute()
        {
        }
    }
}
