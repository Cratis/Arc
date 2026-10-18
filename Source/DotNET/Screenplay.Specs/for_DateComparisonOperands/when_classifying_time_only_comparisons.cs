// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Analysis.Commands;
using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.for_DateComparisonOperands;

public class when_classifying_time_only_comparisons : Specification
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_not_admit_time_only_operands_as_dates(bool concept)
    {
        var source = concept ? ConceptDateComparisonSources.Guard("TimeOnly", "End >= Start") : DateComparisonSources.Guard("TimeOnly", "End >= Start");
        var compilation = Analyzed.Compile((Analyzed.SlicePath, source));
        Analyzed.ErrorsIn(compilation).ShouldBeEmpty();
        var tree = compilation.SyntaxTrees.First();
        var model = compilation.GetSemanticModel(tree);
        var comparison = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();
        DateComparisonOperands.Primitive(model.GetTypeInfo(comparison.Left).Type).ShouldBeNull();
        DateConditions.Applies(comparison, model).ShouldBeFalse();
    }
}
