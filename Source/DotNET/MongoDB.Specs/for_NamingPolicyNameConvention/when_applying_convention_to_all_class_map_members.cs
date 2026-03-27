// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Collections;
using Cratis.Serialization;
using Cratis.Strings;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_NamingPolicyNameConvention;

public class when_applying_convention_to_all_class_map_members : Specification
{
    class SomeType
    {
        public string SomeProperty { get; init; }
        public string SomeOtherProperty { get; init; }
    }

    BsonClassMap<SomeType> _classMap;
    NamingPolicyNameConvention _convention;
    INamingPolicy _namingPolicy;

    void Establish()
    {
        _namingPolicy = Substitute.For<INamingPolicy>();
        _namingPolicy.GetPropertyName(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>().ToCamelCase());

        DatabaseExtensions.SetNamingPolicy(_namingPolicy);
        _classMap = new BsonClassMap<SomeType>();
        _convention = new NamingPolicyNameConvention();
        _classMap.AutoMap();
        _classMap.MapMember(_ => _.SomeOtherProperty).SetElementName("SomeOtherPropertyName");
    }

    void Because() => _classMap.DeclaredMemberMaps.ForEach(_convention.Apply);

    [Fact] void should_have_two_members() => _classMap.DeclaredMemberMaps.Count().ShouldEqual(2);
    [Fact] void should_convert_SomeProperty_to_camelCase() => _classMap.DeclaredMemberMaps.Where(_ => _.ElementName == "someProperty").ShouldContainSingleItem();
    [Fact] void should_convert_SomeOtherProperty_to_someOtherPropertyName() => _classMap.DeclaredMemberMaps.Where(_ => _.ElementName == "someOtherPropertyName").ShouldContainSingleItem();
    [Fact] void should_call_naming_policy_for_SomeProperty() => _namingPolicy.Received().GetPropertyName(nameof(SomeType.SomeProperty));
    [Fact] void should_call_naming_policy_for_SomeOtherProperty() => _namingPolicy.Received().GetPropertyName("SomeOtherPropertyName");
}