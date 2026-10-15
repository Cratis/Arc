// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.for_ConnectionResolutionOptions;

public class when_nothing_is_configured : Specification
{
    [Fact] void should_not_be_configured() => new ConnectionResolutionOptions().IsConfigured.ShouldBeFalse();
}
