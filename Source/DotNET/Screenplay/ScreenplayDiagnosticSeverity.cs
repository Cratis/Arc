// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay;

/// <summary>
/// Represents how serious a <see cref="ScreenplayDiagnostic"/> is.
/// </summary>
public enum ScreenplayDiagnosticSeverity
{
    /// <summary>
    /// Something worth knowing that does not affect the generated document.
    /// </summary>
    Information = 0,

    /// <summary>
    /// Something that could not be expressed and was left out of the generated document.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Something that stopped the document from being generated.
    /// </summary>
    Error = 2
}
