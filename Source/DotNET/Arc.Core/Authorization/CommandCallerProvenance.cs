// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization;

/// <summary>Identifies how the command caller was selected.</summary>
internal enum CommandCallerProvenance
{
    /// <summary>The caller was supplied by the current principal accessor.</summary>
    Ambient = 0,

    /// <summary>An explicit authentication scheme selected the caller.</summary>
    SchemeSelected = 1
}
