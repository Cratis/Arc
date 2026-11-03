// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>The safe reason a caller-derived lookup was refused.</summary>
public enum ReadModelKeyRefusal
{
    /// <summary>No command caller frame is available.</summary>
    NoCaller = 0,

    /// <summary>The captured caller has changed.</summary>
    CallerChanged = 1,

    /// <summary>The provider returned no key.</summary>
    Missing = 2,

    /// <summary>The provider returned multiple keys.</summary>
    Ambiguous = 3,

    /// <summary>The provider failed.</summary>
    ProviderFailed = 4,

    /// <summary>The resolved key text is not safe for an exact lookup.</summary>
    RefusedKeyText = 5
}
