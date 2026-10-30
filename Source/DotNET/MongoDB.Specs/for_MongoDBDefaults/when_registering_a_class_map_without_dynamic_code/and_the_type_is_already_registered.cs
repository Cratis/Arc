// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson.Serialization;

namespace Cratis.Arc.MongoDB.for_MongoDBDefaults.when_registering_a_class_map_without_dynamic_code;

public class and_the_type_is_already_registered : Specification
{
    Exception _exception;
    string _elementName;

    void Establish() =>
        BsonClassMap.RegisterClassMap<AlreadyRegisteredDocument>(_ => _.MapProperty(d => d.Name).SetElementName("first_registration"));

    void Because()
    {
        _exception = Catch.Exception(() => MongoDBDefaults.RegisterClassMapWithoutDynamicCode(typeof(AlreadyRegisteredDocument), new AlreadyRegisteredDocumentClassMap()));
        _elementName = BsonClassMap.LookupClassMap(typeof(AlreadyRegisteredDocument)).GetMemberMap(nameof(AlreadyRegisteredDocument.Name)).ElementName;
    }

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
    [Fact] void should_keep_the_first_registration() => _elementName.ShouldEqual("first_registration");

    public class AlreadyRegisteredDocument
    {
        public string Name { get; set; }
    }

    public class AlreadyRegisteredDocumentClassMap : IBsonClassMapFor<AlreadyRegisteredDocument>
    {
        public void Configure(BsonClassMap<AlreadyRegisteredDocument> classMap) => classMap.MapProperty(_ => _.Name).SetElementName("second_registration");
    }
}
