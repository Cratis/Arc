// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_CommandExtensions.when_converting_to_descriptor_with_skip_name_true;

public class and_single_command_in_namespace : Specification
{
    CommandDescriptor _result;
    TypeInfo[] _allCommands;

    void Establish()
    {
        _allCommands = [typeof(TestTypes.Products.CreateProduct).GetTypeInfo()];
    }

    void Because() => _result = typeof(TestTypes.Products.CreateProduct).GetTypeInfo().ToCommandDescriptor(
        "/output",
        segmentsToSkip: 5,
        skipCommandNameInRoute: true,
        apiPrefix: "api",
        _allCommands);

    [Fact] void should_not_include_command_name_in_route() => _result.Route.ShouldEqual("/api/test-types/products");
}
