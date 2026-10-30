// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson.Serialization;

namespace Cratis.Arc.MongoDB.for_MongoDBDefaults.when_registering_class_maps;

public class and_the_type_is_already_registered : Specification
{
    IMongoDBBuilder _builder;
    Exception _exception;
    string _discriminator;

    void Establish()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(RegisteredDocument)))
        {
            BsonClassMap.RegisterClassMap<RegisteredDocument>(_ => _.SetDiscriminator("first_registration"));
        }

        _builder = Substitute.For<IMongoDBBuilder>();
        _builder.ClassMaps.Returns([typeof(RegisteredDocumentClassMap)]);
    }

    void Because()
    {
        _exception = Catch.Exception(() => MongoDBDefaults.RegisterClassMaps(_builder));
        _discriminator = BsonClassMap.LookupClassMap(typeof(RegisteredDocument)).Discriminator;
    }

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
    [Fact] void should_keep_the_first_registration() => _discriminator.ShouldEqual("first_registration");

    public class RegisteredDocument
    {
        public string Name { get; set; }
    }

    public class RegisteredDocumentClassMap : IBsonClassMapFor<RegisteredDocument>
    {
        public void Configure(BsonClassMap<RegisteredDocument> classMap) => classMap.SetDiscriminator("second_registration");
    }
}
