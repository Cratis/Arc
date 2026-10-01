// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// The codes used for the warnings reported when the board cannot hold everything a document states.
/// </summary>
public static class EventModelWarningCodes
{
    /// <summary>
    /// Something declared at the application level is not drawn.
    /// </summary>
    public const string ApplicationDeclarationNotCarried = "ARCEM0001";

    /// <summary>
    /// Something declared on a slice is not drawn.
    /// </summary>
    public const string SliceConstructNotCarried = "ARCEM0002";

    /// <summary>
    /// Something declared on a command is not drawn.
    /// </summary>
    public const string CommandConstructNotCarried = "ARCEM0003";

    /// <summary>
    /// Something declared on a query is not drawn.
    /// </summary>
    public const string QueryConstructNotCarried = "ARCEM0004";

    /// <summary>
    /// A read model a slice builds is not drawn.
    /// </summary>
    public const string ReadModelNotCarried = "ARCEM0005";
}
