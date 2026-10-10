// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.ModelBound.for_QueryPerformerProvider;

public class when_generated_metadata_coverage_changes_between_containers : Specification
{
    ITypes _types;
    IQueryMetadataRegistry _registry;
    IServiceProviderIsService _serviceProviderIsService;
    IAuthorizationEvaluator _authorizationEvaluator;
    Dictionary<string, Type> _metadata;
    QueryPerformerProvider _scanned;
    QueryPerformerProvider _generated;
    QueryPerformerProvider _scannedAgain;

    void Establish()
    {
        _types = Substitute.For<ITypes>();
        _types.All.Returns([typeof(PublicReadModelWithValidQuery), typeof(PublicReadModelWithInternalQuery)]);
        _registry = Substitute.For<IQueryMetadataRegistry>();
        _serviceProviderIsService = Substitute.For<IServiceProviderIsService>();
        _authorizationEvaluator = Substitute.For<IAuthorizationEvaluator>();
        _metadata = [];
        _registry.All.Returns(_metadata);
    }

    void Because()
    {
        _scanned = Create();
        _metadata[$"Generated.Model.{nameof(PublicReadModelWithValidQuery.GetById)}"] = typeof(PublicReadModelWithValidQuery);
        _generated = Create();
        _metadata.Clear();
        _scannedAgain = Create();
    }

    [Fact] void should_scan_before_metadata_is_available() => _scanned.Performers.Count().ShouldEqual(2);
    [Fact] void should_stop_scanning_the_assembly_when_metadata_is_available() => _generated.Performers.Count().ShouldEqual(1);
    [Fact] void should_use_the_generated_name_when_metadata_is_available() => _generated.TryGetPerformerFor($"Generated.Model.{nameof(PublicReadModelWithValidQuery.GetById)}", out _).ShouldBeTrue();
    [Fact] void should_resume_scanning_when_metadata_is_removed() => _scannedAgain.Performers.Count().ShouldEqual(2);

    QueryPerformerProvider Create() => new(_types, _registry, _serviceProviderIsService, _authorizationEvaluator);
}
