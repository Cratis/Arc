// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.ControllerBased.for_QueryPerformerProvider.given;

public class a_controller_query_performer_provider : Specification
{
    public const string QueryName = "Cratis.Arc.Queries.ControllerBased.for_QueryPerformerProvider.given.TestController.AllEventStores";

    protected IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;
    protected IServiceProviderIsService _serviceProviderIsService;
    protected IAuthorizationEvaluator _authorizationEvaluator;
    protected QueryPerformerProvider _provider;

    void Establish()
    {
        var descriptor1 = new ControllerActionDescriptor
        {
            ActionName = nameof(TestController.AllEventStores),
            ControllerName = nameof(TestController),
            ControllerTypeInfo = typeof(TestController).GetTypeInfo(),
            MethodInfo = typeof(TestController).GetMethod(nameof(TestController.AllEventStores))!
        };

        var descriptor2 = new ControllerActionDescriptor
        {
            ActionName = nameof(TestController.AnonymousEventStores),
            ControllerName = nameof(TestController),
            ControllerTypeInfo = typeof(TestController).GetTypeInfo(),
            MethodInfo = typeof(TestController).GetMethod(nameof(TestController.AnonymousEventStores))!
        };

        _actionDescriptorCollectionProvider = Substitute.For<IActionDescriptorCollectionProvider>();
        _actionDescriptorCollectionProvider.ActionDescriptors.Returns(new ActionDescriptorCollection([descriptor1, descriptor2], 1));

        _serviceProviderIsService = Substitute.For<IServiceProviderIsService>();
        _serviceProviderIsService.IsService(typeof(ITestEventStores)).Returns(true);

        _authorizationEvaluator = Substitute.For<IAuthorizationEvaluator>();
        _authorizationEvaluator.IsAuthorized(Arg.Any<System.Reflection.MethodInfo>()).Returns(true);

        _provider = new QueryPerformerProvider(_actionDescriptorCollectionProvider, _serviceProviderIsService, _authorizationEvaluator);
    }
}