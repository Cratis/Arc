// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Specifications;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.for_SpecificationCaseValues.when_reading;

public class a_value_changing_cast : Specification
{
    bool _read;

    void Because()
    {
        var compilation = Analyzed.Compile(("Cases.cs", "public abstract class Cases(int value) { public byte Read() => (byte)value; }"));
        var tree = compilation.SyntaxTrees.Single(tree => tree.FilePath == "Cases.cs");
        var model = compilation.GetSemanticModel(tree);
        var declaration = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single();
        var parameter = (IParameterSymbol)model.GetDeclaredSymbol(declaration.ParameterList!.Parameters.Single())!;
        var cast = declaration.DescendantNodes().OfType<CastExpressionSyntax>().Single();
        _read = new SpecificationCaseValues([parameter]).TryRead(cast, model, model.GetTypeInfo(cast).Type!, allowsNull: false, out _);
    }

    [Fact] void should_not_replace_the_cast_value_with_the_original_parameter() => _read.ShouldBeFalse();
}
