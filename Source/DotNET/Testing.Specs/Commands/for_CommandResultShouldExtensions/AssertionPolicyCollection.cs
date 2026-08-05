// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.for_CommandResultShouldExtensions;

/// <summary>
/// Runs the assertion-policy specs one at a time.
/// </summary>
/// <remarks>
/// Policies are discovered once per process, so the recording policy is a single instance shared by every spec that
/// observes it. Left to run in parallel, one spec's assertions land in another's recording and one spec's reset
/// clears another's. Serializing them is the honest fix: the seam really is process-wide, and pretending otherwise
/// in the specs would hide that from a reader.
/// </remarks>
[CollectionDefinition(Name, DisableParallelization = true)]
public static class AssertionPolicyCollection
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public const string Name = "AssertionPolicy";
}
