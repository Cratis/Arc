// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Authorization;
using Cratis.Execution;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.ControllerBased.for_ControllerQueryPerformer.when_performing;

public class with_a_verdict_but_no_authorization_declarations : Specification
{
    Exception? _error;
    bool _invoked;

    async Task Because()
    {
        var accessor = Substitute.For<ICurrentPrincipalAccessor>();
        var services = new ServiceCollection().AddSingleton(accessor).BuildServiceProvider();
        var descriptor = new ControllerActionDescriptor
        {
            ActionName = nameof(ProtectedController.Load),
            ControllerName = nameof(ProtectedController),
            ControllerTypeInfo = typeof(ProtectedController).GetTypeInfo(),
            MethodInfo = typeof(ProtectedController).GetMethod(nameof(ProtectedController.Load))!
        };
        var performer = new ControllerQueryPerformer(
            descriptor, services.GetRequiredService<IServiceProviderIsService>(), Substitute.For<IAuthorizationEvaluator>());
        var declarations = new AuthorizationDeclarations(
            new KnownInstancesOf<IAnonymousEvaluator>([]),
            new KnownInstancesOf<IAuthorizationAttributeEvaluator>([]));
        var context = new QueryContext(performer.FullyQualifiedName, CorrelationId.New(), Paging.NotPaged, Sorting.None, QueryArguments.Empty, [services])
        {
            AuthorizedExecution = new AuthorizedExecution(
                performer.AuthorizationMethod,
                AuthorizationPrincipalIdentity.Capture(null),
                declarations.For(performer.AuthorizationMethod),
                false)
        };
        ProtectedController.OnInvoke = () => _invoked = true;
        try
        {
            _error = await Catch.Exception(async () => await performer.Perform(context));
        }
        finally
        {
            ProtectedController.OnInvoke = null;
        }
    }

    [Fact] void should_fail_closed_when_declarations_are_missing() => _error.ShouldBeOfExactType<InvalidAuthorizationConfiguration>();
    [Fact] void should_not_invoke_the_action() => _invoked.ShouldBeFalse();

    public class ProtectedController
    {
        public static Action? OnInvoke { get; set; }
        public object Load()
        {
            OnInvoke?.Invoke();
            return new();
        }
    }
}
