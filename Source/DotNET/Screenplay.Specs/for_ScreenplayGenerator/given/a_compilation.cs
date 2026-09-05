// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

/// <summary>
/// A compilation built from source strings, which is all the generator ever needs - no workspace, no project file
/// and no build output.
/// </summary>
/// <remarks>
/// The source compiles and declares nothing, which are two different things. Analysis tells them apart, so a
/// specification about declaring nothing has to start from source that really does build.
/// </remarks>
public class a_compilation : Specification
{
    protected Compilation _compilation;

    void Establish() => _compilation = Analyzed.Compile(("Library/Authors/Registration/Registration.cs", "namespace Library.Authors.Registration;"));
}
