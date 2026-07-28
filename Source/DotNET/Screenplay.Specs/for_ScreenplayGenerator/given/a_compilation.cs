// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

/// <summary>
/// A compilation built from source strings, which is all the generator ever needs - no workspace, no project file
/// and no build output.
/// </summary>
public class a_compilation : Specification
{
    protected Compilation _compilation;

    void Establish() => _compilation = CSharpCompilation.Create(
        "Library",
        [CSharpSyntaxTree.ParseText("namespace Library.Authors.Registration;")]);
}
