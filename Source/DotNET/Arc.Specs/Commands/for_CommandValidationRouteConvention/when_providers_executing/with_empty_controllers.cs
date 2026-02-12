// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Cratis.Arc.Commands.for_CommandValidationRouteConvention.when_providers_executing;

public class with_empty_controllers : given.a_command_validation_route_convention
{
    ApplicationModel _applicationModel;

    void Establish()
    {
        _applicationModel = new ApplicationModel();
        var controller = new ControllerModel(typeof(TestController).GetTypeInfo(), []);
        _applicationModel.Controllers.Add(controller);
    }

    void Because()
    {
        var context = new given.TestApplicationModelProviderContext(_applicationModel);
        _convention.OnProvidersExecuting(context);
    }

    [Fact] void should_not_throw() => true.ShouldBeTrue();

    class TestController : ControllerBase;
}
