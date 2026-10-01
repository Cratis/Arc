// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Catalog;

/// <summary>
/// Exception that gets thrown when the catalog embedded in an assembly cannot be read as written.
/// </summary>
/// <remarks>
/// A catalog that cannot be read is a broken package, not a missing feature. Mapping the explorer fails with
/// this exception rather than serving a partial tree that silently omits what the assembly actually holds.
/// </remarks>
/// <param name="assemblyName">The name of the assembly holding the catalog.</param>
/// <param name="reason">The reason the catalog could not be read.</param>
/// <param name="innerException">Optional inner exception describing the failure in detail.</param>
public class MalformedEventModelCatalog(string assemblyName, string reason, Exception? innerException = null)
    : Exception($"The embedded Screenplay catalog in '{assemblyName}' is malformed - {reason}.", innerException);
