// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_ResponseTypeTraversal;

public class when_bounding_a_signature_walk : Specification
{
    ITypeSymbol[] _types;

    async Task Establish()
    {
        var compilation = (await TestProject.CreateProject("public class A; public class B; public class C;")
            .GetCompilationAsync(TestContext.Current.CancellationToken))!;
        _types = [compilation.GetTypeByMetadataName("A")!, compilation.GetTypeByMetadataName("B")!, compilation.GetTypeByMetadataName("C")!];
    }

    [Fact]
    public void should_cut_a_branching_mutual_cycle_before_reexpanding_an_active_symbol()
    {
        var traversal = new ResponseTypeTraversal();
        var inspected = 0;
        Visit(_types[0]);
        inspected.ShouldEqual(5);
        void Visit(ITypeSymbol type) => traversal.Visit(
            type,
            () =>
            {
                inspected++;
                foreach (var child in _types.Where(child => !SymbolEqualityComparer.Default.Equals(child, type)))
                {
                    Visit(child);
                }
                return true;
            },
            false);
    }

    [Fact]
    public void should_ignore_nullable_annotations_when_detecting_an_active_type()
    {
        var traversal = new ResponseTypeTraversal();
        traversal.Visit(_types[0], () => traversal.Visit(_types[0].WithNullableAnnotation(NullableAnnotation.Annotated), () => true, false), false)
            .ShouldBeFalse();
    }

    [Fact]
    public void should_keep_sibling_contexts_independent()
    {
        var traversal = new ResponseTypeTraversal();
        traversal.Visit(_types[0], () => "first", "skipped").ShouldEqual("first");
        traversal.Visit(_types[0], () => "second", "skipped").ShouldEqual("second");
    }

    [Fact]
    public void should_allow_a_distinct_inherited_context_without_reentering_that_context()
    {
        var traversal = new ResponseTypeTraversal();
        traversal.Visit(_types[0], () => traversal.Visit(_types[0], () => true, false, inheritedContext: true), false).ShouldBeTrue();
        traversal.Visit(_types[0], () => traversal.Visit(_types[0], () => true, false, inheritedContext: true), false, inheritedContext: true).ShouldBeFalse();
    }

    [Fact]
    public void should_keep_a_depth_backstop()
    {
        var traversal = new ResponseTypeTraversal(maximumDepth: 2);
        traversal.Visit(_types[0], () => traversal.Visit(_types[1], () => traversal.Visit(_types[2], () => true, false), false), false)
            .ShouldBeFalse();
    }

    [Fact]
    public void should_bound_total_work_even_without_a_cycle()
    {
        var traversal = new ResponseTypeTraversal();
        var inspected = 0;
        for (var index = 0; index < 5000; index++)
        {
            traversal.Visit(_types[0], () => ++inspected, 0);
        }
        inspected.ShouldBeLessThan(5000);
    }
}
