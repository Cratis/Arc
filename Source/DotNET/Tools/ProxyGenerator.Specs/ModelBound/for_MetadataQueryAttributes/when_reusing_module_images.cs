// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryMethod.given;
using ValidReadModel = Cratis.Arc.ProxyGenerator.ModelBound.for_TypeExtensionsModelBound.ValidReadModel;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_MetadataQueryAttributes;

public class when_reusing_module_images : given.a_metadata_discovery
{
    string[] _explicit;
    string[] _conventional;
    string[] _uncachedExplicit;
    string[] _uncachedConventional;
    Type _explicitOwner;
    bool _legacyOwner;
    CustomAttributeData _simpleMarker;
    CustomAttributeData _exactMarker;

    void Establish()
    {
        _explicitOwner = MetadataType<SelectedReadModel>();
        _uncachedExplicit = _explicitOwner.GetQueryMethods().Select(_ => _.Name).ToArray();
        _uncachedConventional = MetadataType<ConventionalReadModel>().GetQueryMethods().Select(_ => _.Name).ToArray();
    }

    void Because()
    {
        _explicit = _explicitOwner.GetQueryMethods(_attributes.Find).Select(_ => _.Name).ToArray();
        _conventional = MetadataType<ConventionalReadModel>().GetQueryMethods(_attributes.Find).Select(_ => _.Name).ToArray();
        _legacyOwner = MetadataType<ValidReadModel>().IsQuery(_attributes.Find);
        _simpleMarker = _attributes.Find(_explicitOwner, "ReadModelAttribute", false)!;
        _exactMarker = _attributes.Find(_explicitOwner, typeof(ReadModelAttribute).FullName!)!;
        _ = _explicitOwner.GetTypeInfo().ToQueryDescriptors("output", 0, true, "api", [_explicitOwner.GetTypeInfo()], _attributes.Find).ToArray();
        _ = _explicitOwner.GetQueryMethods(_attributes.Find).ToArray();
        _ = _attributes.Find(_context.LoadFromAssemblyPath(typeof(QueryAttribute).Assembly.Location).GetType(typeof(QueryAttribute).FullName!)!, "MissingAttribute");
    }

    [Fact] void should_preserve_explicit_selection() => _explicit.ShouldContainOnly("ById", "Observe");
    [Fact] void should_match_uncached_explicit_selection() => _explicit.SequenceEqual(_uncachedExplicit).ShouldBeTrue();
    [Fact] void should_preserve_conventional_selection() => _conventional.ShouldContainOnly("Marked", "Create", "Sibling");
    [Fact] void should_match_uncached_conventional_selection() => _conventional.SequenceEqual(_uncachedConventional).ShouldBeTrue();
    [Fact] void should_preserve_legacy_simple_name_owner_matching() => _legacyOwner.ShouldBeTrue();
    [Fact] void should_match_the_same_marker_by_exact_and_simple_names() => _simpleMarker.AttributeType.ShouldEqual(_exactMarker.AttributeType);
    [Fact] void should_open_each_module_only_once() => _openedPaths.GroupBy(_ => _).All(_ => _.Count() == 1).ShouldBeTrue();
    [Fact] void should_open_the_owner_module() => _openedPaths.ShouldContain(_explicitOwner.Module.FullyQualifiedName);
    [Fact] void should_open_other_modules_independently() => _openedPaths.ShouldContain(typeof(QueryAttribute).Module.FullyQualifiedName);
}
