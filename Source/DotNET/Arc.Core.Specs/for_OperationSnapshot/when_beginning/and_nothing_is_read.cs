// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.for_OperationSnapshot.when_beginning;

public class and_nothing_is_read : given.an_operation_snapshot_environment
{
    void Because()
    {
        using var snapshot = OperationSnapshot.Begin(_services, () => null);
    }

    [Fact] void should_not_resolve_the_tenant() => _resolver.DidNotReceive().Resolve();
}
