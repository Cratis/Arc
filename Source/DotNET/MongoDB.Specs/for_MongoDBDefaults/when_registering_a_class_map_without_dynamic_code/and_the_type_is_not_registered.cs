// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson.Serialization;

namespace Cratis.Arc.MongoDB.for_MongoDBDefaults.when_registering_a_class_map_without_dynamic_code;

public class and_the_type_is_not_registered : Specification
{
    string _elementName;

    void Because()
    {
        MongoDBDefaults.RegisterClassMapWithoutDynamicCode(typeof(UnregisteredDocument), new UnregisteredDocumentClassMap());
        _elementName = BsonClassMap.LookupClassMap(typeof(UnregisteredDocument)).GetMemberMap(nameof(UnregisteredDocument.Name)).ElementName;
    }

    [Fact] void should_register_the_class_map() => BsonClassMap.IsClassMapRegistered(typeof(UnregisteredDocument)).ShouldBeTrue();
    [Fact] void should_configure_the_class_map() => _elementName.ShouldEqual("configured_name");

    public class UnregisteredDocument
    {
        public string Name { get; set; }
    }

    public class UnregisteredDocumentClassMap : IBsonClassMapFor<UnregisteredDocument>
    {
        public void Configure(BsonClassMap<UnregisteredDocument> classMap) => classMap.MapProperty(_ => _.Name).SetElementName("configured_name");
    }
}
