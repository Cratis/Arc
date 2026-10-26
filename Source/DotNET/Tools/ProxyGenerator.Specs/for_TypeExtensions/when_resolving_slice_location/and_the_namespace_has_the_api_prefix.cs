// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.API.Billing.Invoices.Pay;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_resolving_slice_location;

[Collection(AssemblyPackageMappingCollectionDefinition.Name)]
public class and_the_namespace_has_the_api_prefix : Specification
{
    SliceLocationDescriptor _result = null!;
    string _outputFolder = string.Empty;

    void Establish() => TypeExtensions.SetNamespaceRoots([]);

    void Because()
    {
        _result = typeof(TypeUnderTheApiNamespacePrefix).ResolveSliceLocation(1);
        _outputFolder = typeof(TypeUnderTheApiNamespacePrefix).ResolveTargetPath(1);
    }

    [Fact] void should_not_include_the_prefix_in_the_module() => _result.Module.ShouldEqual("Billing");
    [Fact] void should_resolve_the_feature() => _result.Feature.ShouldEqual("Invoices");
    [Fact] void should_resolve_the_slice() => _result.Slice.ShouldEqual("Pay");
    [Fact] void should_match_the_output_folder() =>
        _outputFolder.ShouldEqual(Path.Join(_result.Module, _result.Feature, _result.Slice));
}
