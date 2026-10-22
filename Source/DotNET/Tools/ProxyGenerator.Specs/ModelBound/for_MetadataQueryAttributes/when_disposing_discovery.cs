// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection.Metadata;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryMethod.given;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_MetadataQueryAttributes;

public class when_disposing_discovery : given.a_metadata_discovery
{
    Type _owner;

    void Establish()
    {
        _owner = MetadataType<SelectedReadModel>();
        _attributes.Find(_owner, typeof(ReadModelAttribute).FullName!);
        _attributes.Find(_context.LoadFromAssemblyPath(typeof(QueryAttribute).Assembly.Location).GetType(typeof(QueryAttribute).FullName!)!, "MissingAttribute");
    }

    void Because()
    {
        _attributes.Dispose();
        _attributes.Dispose();
    }

    [Fact] void should_have_opened_multiple_images() => (_images.Count >= 2).ShouldBeTrue();
    [Fact] void should_dispose_every_opened_image() => _images.TrueForAll(image => Catch.Exception(() => image.GetMetadataReader()) is ObjectDisposedException).ShouldBeTrue();
    [Fact] void should_refuse_probing_after_disposal() => Catch.Exception(() => _attributes.Find(_owner, typeof(ReadModelAttribute).FullName!)).ShouldBeOfExactType<ObjectDisposedException>();
}
