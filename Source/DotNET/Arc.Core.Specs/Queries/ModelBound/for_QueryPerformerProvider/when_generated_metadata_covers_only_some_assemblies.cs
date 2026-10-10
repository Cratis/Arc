// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;
using Cratis.Arc.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.ModelBound.for_QueryPerformerProvider;

public class when_generated_metadata_covers_only_some_assemblies : Specification
{
    QueryPerformerProvider _provider;
    ITypes _types;
    IQueryMetadataRegistry _registry;
    IServiceProviderIsService _serviceProviderIsService;
    IAuthorizationEvaluator _authorizationEvaluator;
    Type _scannedReadModel;
    string _generatedQueryName;
    string _scannedQueryName;

    void Establish()
    {
        _types = Substitute.For<ITypes>();
        _registry = Substitute.For<IQueryMetadataRegistry>();
        _serviceProviderIsService = Substitute.For<IServiceProviderIsService>();
        _authorizationEvaluator = Substitute.For<IAuthorizationEvaluator>();
        _scannedReadModel = CreateReadModelWithoutGeneratedMetadata();
        _generatedQueryName = $"{typeof(PublicReadModelWithValidQuery).FullName}.{nameof(PublicReadModelWithValidQuery.GetById)}";
        _scannedQueryName = $"{_scannedReadModel.FullName}.Query";
        _types.All.Returns([typeof(PublicReadModelWithValidQuery), typeof(PublicReadModelWithInternalQuery), _scannedReadModel, _scannedReadModel]);
        _registry.All.Returns(new Dictionary<string, Type>
        {
            [_generatedQueryName] = typeof(PublicReadModelWithValidQuery)
        });
    }

    void Because() => _provider = new QueryPerformerProvider(_types, _registry, _serviceProviderIsService, _authorizationEvaluator);

    [Fact] void should_keep_the_generated_query() => _provider.TryGetPerformerFor(_generatedQueryName, out _).ShouldBeTrue();
    [Fact] void should_discover_the_query_from_the_other_assembly() => _provider.TryGetPerformerFor(_scannedQueryName, out _).ShouldBeTrue();
    [Fact] void should_resolve_duplicate_query_names_only_once() => _provider.Performers.Count().ShouldEqual(2);
    [Fact] void should_not_scan_the_assembly_covered_by_generated_metadata() => _provider.TryGetPerformerFor($"{typeof(PublicReadModelWithInternalQuery).FullName}.Query", out _).ShouldBeFalse();

    static Type CreateReadModelWithoutGeneratedMetadata()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"ScannedQueries_{Guid.NewGuid():N}"), AssemblyBuilderAccess.RunAndCollect);
        var module = assembly.DefineDynamicModule("Queries");
        var type = module.DefineType("Scanned.ReadModel", TypeAttributes.Public);
        type.SetCustomAttribute(new CustomAttributeBuilder(typeof(ReadModelAttribute).GetConstructor(Type.EmptyTypes)!, []));
        var method = type.DefineMethod("Query", MethodAttributes.Public | MethodAttributes.Static, type, Type.EmptyTypes);
        var body = method.GetILGenerator();
        body.Emit(OpCodes.Ldnull);
        body.Emit(OpCodes.Ret);
        return type.CreateType()!;
    }
}
