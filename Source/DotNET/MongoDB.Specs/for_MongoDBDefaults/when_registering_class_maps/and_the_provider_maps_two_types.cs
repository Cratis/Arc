// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson.Serialization;

namespace Cratis.Arc.MongoDB.for_MongoDBDefaults.when_registering_class_maps;

public class and_the_provider_maps_two_types : Specification
{
    IMongoDBBuilder _builder;
    string _firstDiscriminator;
    string _secondDiscriminator;

    void Establish()
    {
        _builder = Substitute.For<IMongoDBBuilder>();
        _builder.ClassMaps.Returns([typeof(TwoClassMaps)]);
    }

    void Because()
    {
        MongoDBDefaults.RegisterClassMaps(_builder);
        _firstDiscriminator = BsonClassMap.LookupClassMap(typeof(FirstDocument)).Discriminator;
        _secondDiscriminator = BsonClassMap.LookupClassMap(typeof(SecondDocument)).Discriminator;
    }

    [Fact] void should_register_the_first_type() => BsonClassMap.IsClassMapRegistered(typeof(FirstDocument)).ShouldBeTrue();
    [Fact] void should_register_the_second_type() => BsonClassMap.IsClassMapRegistered(typeof(SecondDocument)).ShouldBeTrue();
    [Fact] void should_configure_the_first_type() => _firstDiscriminator.ShouldEqual("first_name");
    [Fact] void should_configure_the_second_type() => _secondDiscriminator.ShouldEqual("second_name");

    public class FirstDocument
    {
        public string Name { get; set; }
    }

    public class SecondDocument
    {
        public string Name { get; set; }
    }

    public class TwoClassMaps : IBsonClassMapFor<FirstDocument>, IBsonClassMapFor<SecondDocument>
    {
        public void Configure(BsonClassMap<FirstDocument> classMap) => classMap.SetDiscriminator("first_name");

        public void Configure(BsonClassMap<SecondDocument> classMap) => classMap.SetDiscriminator("second_name");
    }
}
