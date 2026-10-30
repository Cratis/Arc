// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson.Serialization;

namespace Cratis.Arc.MongoDB.for_MongoDBDefaults.when_registering_class_maps;

public class and_the_type_is_already_registered : Specification
{
    IMongoDBBuilder _builder;
    Exception _exception;
    string _elementName;

    void Establish()
    {
        BsonClassMap.RegisterClassMap<RegisteredDocument>(_ => _.MapProperty(d => d.Name).SetElementName("first_registration"));
        _builder = Substitute.For<IMongoDBBuilder>();
        _builder.ClassMaps.Returns([typeof(RegisteredDocumentClassMap)]);
    }

    void Because()
    {
        _exception = Catch.Exception(() => MongoDBDefaults.RegisterClassMaps(_builder));
        _elementName = BsonClassMap.LookupClassMap(typeof(RegisteredDocument)).GetMemberMap(nameof(RegisteredDocument.Name)).ElementName;
    }

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
    [Fact] void should_keep_the_first_registration() => _elementName.ShouldEqual("first_registration");

    public class RegisteredDocument
    {
        public string Name { get; set; }
    }

    public class RegisteredDocumentClassMap : IBsonClassMapFor<RegisteredDocument>
    {
        public void Configure(BsonClassMap<RegisteredDocument> classMap) => classMap.MapProperty(_ => _.Name).SetElementName("second_registration");
    }
}
