// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator;

/// <summary>Defines command-line outcomes.</summary>
internal static class ProxyGeneratorExitCodes
{
    /// <summary>Successful generation or completed comparison without drift.</summary>
    public const int Clean = 0;

    /// <summary>Generation, comparison or usage failed.</summary>
    public const int Error = 1;

    /// <summary>A completed comparison found drift.</summary>
    public const int Drift = 2;
}
