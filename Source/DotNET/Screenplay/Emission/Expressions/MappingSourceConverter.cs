// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission.Expressions;

/// <summary>
/// Converts a <see cref="MappingSourceModel"/> into the host language expression it corresponds to.
/// </summary>
/// <param name="naming">The <see cref="IScreenplayNaming"/> used for name conversion.</param>
/// <remarks>
/// This is the host expression grammar, which is what <c>produces</c> mappings, conditions and validation operands
/// are parsed with. It is not the same grammar as the one inside a projection body - crossing the two produces
/// output that does not compile.
/// </remarks>
public class MappingSourceConverter(IScreenplayNaming naming)
{
    /// <summary>
    /// Converts a mapping source.
    /// </summary>
    /// <param name="source">The source to convert.</param>
    /// <returns>The <see cref="ExpressionSyntax"/>.</returns>
    public ExpressionSyntax Convert(MappingSourceModel source) => source switch
    {
        PropertyPathSource path => new PathExpressionSyntax(naming.ToPropertyPath(path.Path), SourceLocation.Start),
        ContextSource context => new ContextExpressionSyntax(naming.ToPropertyPath(context.Path), SourceLocation.Start),
        LiteralSource literal => LiteralConverter.Convert(literal.Value, naming),
        _ => LiteralConverter.Convert(null, naming)
    };
}
