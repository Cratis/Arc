// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Files;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission.Constraints;

/// <summary>
/// Builds the Screenplay <c>constraint</c> declaration for a constraint.
/// </summary>
/// <param name="naming">The <see cref="IScreenplayNaming"/> used for name conversion.</param>
/// <remarks>
/// Screenplay knows exactly three constraint shapes - unique on a property of an event, unique on an event type, and
/// a reference to the file holding the rule. Anything else is rendered as a file reference rather than as something
/// it is not.
/// </remarks>
public class ConstraintSyntaxBuilder(IScreenplayNaming naming)
{
    /// <summary>
    /// Builds the constraint declaration.
    /// </summary>
    /// <param name="constraint">The constraint to build for.</param>
    /// <param name="namespace">The namespace of the slice the constraint lives in.</param>
    /// <returns>The <see cref="ConstraintSyntax"/>.</returns>
    public ConstraintSyntax Build(ConstraintModel constraint, string @namespace)
    {
        var name = naming.ToDeclarationName(constraint.Name);

        return constraint switch
        {
            UniquePropertyConstraintModel unique => new UniquePropertyConstraintSyntax(
                name,
                naming.ToPropertyPath(unique.Property),
                naming.ToDeclarationName(unique.EventName),
                SourceLocation.Start),
            UniqueEventConstraintModel unique => new UniqueEventConstraintSyntax(
                name,
                naming.ToDeclarationName(unique.EventName),
                SourceLocation.Start),
            CustomConstraintModel custom => ToFileConstraint(name, custom, @namespace),
            _ => ToFileConstraint(name, new CustomConstraintModel(constraint.Name, null), @namespace)
        };
    }

    /// <summary>
    /// Builds the declaration pointing at the file holding a constraint's rule.
    /// </summary>
    /// <param name="name">The sanitized name of the constraint.</param>
    /// <param name="constraint">The constraint to build for.</param>
    /// <param name="namespace">The namespace of the slice the constraint lives in.</param>
    /// <returns>The <see cref="FileConstraintSyntax"/>.</returns>
    FileConstraintSyntax ToFileConstraint(string name, CustomConstraintModel constraint, string @namespace)
    {
        var path = naming.ToFilePath(constraint.SourceFilePath) ?? SourceFilePaths.Conventional(@namespace, name);

        return new(name, new FileReferenceSyntax(path, SourceLocation.Start), SourceLocation.Start);
    }
}
