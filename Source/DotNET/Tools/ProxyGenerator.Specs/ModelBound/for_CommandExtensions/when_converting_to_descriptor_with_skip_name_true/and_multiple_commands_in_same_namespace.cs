// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_CommandExtensions.when_converting_to_descriptor_with_skip_name_true;

public class and_multiple_commands_in_same_namespace : Specification
{
    CommandDescriptor _createOrderResult;
    CommandDescriptor _updateOrderResult;
    TypeInfo[] _allCommands;

    void Establish()
    {
        _allCommands =
        [
            typeof(TestTypes.Orders.CreateOrder).GetTypeInfo(),
            typeof(TestTypes.Orders.UpdateOrder).GetTypeInfo()
        ];
    }

    void Because()
    {
        _createOrderResult = typeof(TestTypes.Orders.CreateOrder).GetTypeInfo().ToCommandDescriptor(
            "/output",
            segmentsToSkip: 5,
            skipCommandNameInRoute: true,
            apiPrefix: "api",
            _allCommands);

        _updateOrderResult = typeof(TestTypes.Orders.UpdateOrder).GetTypeInfo().ToCommandDescriptor(
            "/output",
            segmentsToSkip: 5,
            skipCommandNameInRoute: true,
            apiPrefix: "api",
            _allCommands);
    }

    [Fact] void should_include_create_order_command_name_in_route() => _createOrderResult.Route.ShouldEqual("/api/test-types/orders/create-order");
    [Fact] void should_include_update_order_command_name_in_route() => _updateOrderResult.Route.ShouldEqual("/api/test-types/orders/update-order");
}
