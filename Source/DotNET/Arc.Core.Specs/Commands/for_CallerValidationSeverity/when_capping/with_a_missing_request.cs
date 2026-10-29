// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CallerValidationSeverity.when_capping;

public class with_a_missing_request : given.a_caller_request
{
    void Establish() => _requested = null;
    void Because() => Cap();

    [Fact] void should_keep_the_missing_request() => _result.ShouldBeNull();
}
