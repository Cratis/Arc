// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Hosting.Board;
using Cratis.Arc.Screenplay.Embedded.Hosting.Catalog;

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelEndpoints;

public class when_compiling_concurrently : Specification
{
    CompiledEventModels _models;
    string _project;
    string _document;
    EventModelView?[] _results;

    void Establish()
    {
        var catalog = EventModelCatalog.For([typeof(Company.Library.Program).Assembly]);
        var project = catalog.Projects.Single();
        _project = project.Id;
        _document = project.Documents[0].Id;
        _models = new(catalog);
    }

    async Task Because() => _results = await Task.WhenAll(Enumerable.Range(0, 12).Select(_ => Task.Run(() =>
    {
        _models.TryGet(_project, _document, out var model);
        return model;
    })));

    [Fact] void should_compile_successfully_for_every_caller() => _results.All(_ => _?.Success == true).ShouldBeTrue();
    [Fact] void should_reuse_the_same_immutable_model() => _results.All(_ => ReferenceEquals(_, _results[0])).ShouldBeTrue();
}
