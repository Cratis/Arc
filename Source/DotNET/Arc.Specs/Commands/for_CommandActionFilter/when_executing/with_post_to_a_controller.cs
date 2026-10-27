// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands.for_CommandActionFilter.given.Billing;
using Cratis.Arc.Slices;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Cratis.Arc.Commands.for_CommandActionFilter.when_executing;

public class with_post_to_a_controller : given.a_post_request_for_a_command_action
{
    CommandContext _capturedContext;

    void Establish()
    {
        ((ControllerActionDescriptor)_actionContext.ActionDescriptor).ControllerTypeInfo = typeof(InvoicesController).GetTypeInfo();
        _contextModifier
            .When(m => m.SetCurrent(Arg.Any<CommandContext>()))
            .Do(call => _capturedContext = call.Arg<CommandContext>());
    }

    async Task Because() => await _filter.OnActionExecutionAsync(_actionContext, () => Task.FromResult(_executedContext));

    [Fact] void should_resolve_the_slice_location_from_the_controller() => _capturedContext.SliceLocation.ShouldEqual(SliceLocation.For(typeof(InvoicesController)));
    [Fact] void should_end_the_slice_location_at_the_controller_namespace() => _capturedContext.SliceLocation.Slice.Value.ShouldEqual("Billing");
}
