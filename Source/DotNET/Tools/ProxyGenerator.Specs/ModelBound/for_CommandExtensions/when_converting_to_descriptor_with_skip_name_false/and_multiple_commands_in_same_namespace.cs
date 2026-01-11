// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_CommandExtensions.when_converting_to_descriptor_with_skip_name_false;

public class and_multiple_commands_in_same_namespace : Specification
{
    CommandDescriptor _createOrderResult;
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
            skipCommandNameInRoute: false,
            apiPrefix: "api",
            _allCommands);
    }

    [Fact] void should_include_command_name_in_route_regardless_of_conflict() => _createOrderResult.Route.ShouldEqual("/api/testtypes/orders/create-order");
}
