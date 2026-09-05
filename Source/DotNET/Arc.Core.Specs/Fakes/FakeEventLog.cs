// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Namespace deliberately mimics a Chronicle type so the Chronicle configuration hint detection (which is
// namespace-based to keep Arc free of a Chronicle reference) can be exercised without depending on Chronicle.
namespace Cratis.Chronicle.Specs.Fakes;

/// <summary>
/// Stands in for a Chronicle service (such as IEventLog) whose own dependency is not registered when
/// WithChronicle() was never called.
/// </summary>
/// <param name="name">A dependency that is intentionally left unregistered in the spec.</param>
public class FakeEventLog(FakeEventStoreName name)
{
    /// <summary>
    /// Gets the injected dependency.
    /// </summary>
    public FakeEventStoreName Name => name;
}
