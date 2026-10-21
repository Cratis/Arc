// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;
using Cratis.Chronicle;

namespace Cratis.Arc.Chronicle.Occurrences.for_OccurrenceCommandExecutor;

public class when_the_occurrence_is_for_a_tenant_namespace : given.an_occurrence_command_executor
{
    void Establish()
    {
        _occurrenceNamespace = new EventStoreNamespaceName("acme");
        CreateExecutor();
    }

    async Task Because() => _exception = await Catch.Exception(() => _executor.Execute(_command, _occurrence, "send-reminder"));

    [Fact] void should_select_the_tenant_of_the_namespace() => _tenantScope.Received(1).Begin(Arg.Is<TenantId>(_ => _.Value == "acme"));
}
