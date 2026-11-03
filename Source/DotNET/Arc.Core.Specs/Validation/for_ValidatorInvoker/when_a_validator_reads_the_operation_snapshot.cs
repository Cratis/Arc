// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Validation.for_ValidatorInvoker;

public class when_a_validator_reads_the_operation_snapshot : for_OperationSnapshot.given.an_operation_snapshot_environment
{
    ValidatorInvoker _invoker;
    Exception _withoutSnapshot;
    IEnumerable<ValidationResult> _results;

    async Task Because()
    {
        _invoker = new ValidatorInvoker(Microsoft.Extensions.Logging.Abstractions.NullLogger<ValidatorInvoker>.Instance);
        ReadingValidator.Tenants.Clear();
        _withoutSnapshot = await Catch.Exception(async () => await _invoker.Invoke(new Subject("a", "b"), new ReadingValidator(), string.Empty));

        using var snapshot = OperationSnapshot.Begin(_services, () => null);
        _results = await _invoker.Invoke(new Subject("a", "b"), new ReadingValidator(), string.Empty);
    }

    [Fact] void should_propagate_when_there_is_no_snapshot() => _withoutSnapshot.ShouldBeOfExactType<OperationSnapshotUnavailable>();
    [Fact] void should_validate_cleanly_with_a_snapshot() => _results.ShouldBeEmpty();
    [Fact] void should_share_the_snapshot_with_child_validators() => ReadingValidator.Tenants.ShouldEqual(["tenant-1", "tenant-1"]);
}
