// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc;

/// <summary>
/// Collection definition for specs that set process-wide <see cref="AppContext"/> switches, which other specs read.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class MutatesAppContextSwitchesCollection
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public const string Name = "MutatesAppContextSwitches";
}
