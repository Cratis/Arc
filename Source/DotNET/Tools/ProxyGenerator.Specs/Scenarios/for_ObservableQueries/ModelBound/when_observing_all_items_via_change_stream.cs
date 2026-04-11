// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ObservableQueries.ModelBound;

[Collection(ScenarioCollectionDefinition.Name)]
public class when_observing_all_items_via_change_stream : given.a_scenario_web_application
{
    string _generatedProxy = string.Empty;

    void Establish()
    {
        LoadQueryProxy<ObservableReadModel>("ObserveAll", "/tmp/observe-all-change-stream-proxy.ts");
        _generatedProxy = File.ReadAllText("/tmp/observe-all-change-stream-proxy.ts");
    }

    async Task Because() => await Task.CompletedTask;

    [Fact] void should_expose_use_change_stream_static_method() => _generatedProxy.ShouldContain("useChangeStream");
    [Fact] void should_import_change_set_type() => _generatedProxy.ShouldContain("ChangeSet");
    [Fact] void should_import_use_change_stream_hook() => _generatedProxy.ShouldContain("useChangeStream");
    [Fact] void should_have_static_method_declaration() => _generatedProxy.ShouldContain("static useChangeStream");
}
