// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Queries;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_QueryReader;

public class when_selecting_public_queries : Specification
{
    IMethodSymbol[] _methods;

    void Because()
    {
        var compilation = Analyzed.Compile((Analyzed.SlicePath, """
            using Cratis.Arc.Queries.ModelBound;
            namespace Library.Feature.Slice;
            [ReadModel] internal class Order
            {
                public static Order Public() => new();
                internal static Order Internal() => new();
                private static Order Private() => new();
                protected static Order Protected() => new();
                protected internal static Order ProtectedInternal() => new();
                private protected static Order PrivateProtected() => new();
            }
            """));
        Analyzed.ErrorsIn(compilation).ShouldBeEmpty();
        _methods = QueryReader.MethodsOf(compilation.GetTypeByMetadataName("Library.Feature.Slice.Order")!).ToArray();
    }

    [Fact] void should_select_only_public_methods_even_on_an_internal_type() => _methods.Select(method => method.Name).ShouldContainOnly("Public");
}
