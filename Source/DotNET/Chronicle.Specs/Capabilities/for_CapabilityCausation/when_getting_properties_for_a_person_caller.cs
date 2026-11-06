// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Capabilities;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Chronicle.Capabilities.for_CapabilityCausation;

public class when_getting_properties_for_a_person_caller : Specification
{
    IDictionary<string, string> _properties;
    void Because() => _properties = CapabilityCausation.PropertiesFor(new("capabilities", "rest", CapabilityPrincipalKind.Person, "person", null, null, "credential", null, TenantId.NotSet, new HashSet<string>()));
    [Fact] void should_only_record_present_values() => _properties.Keys.ShouldContainOnly("endpointGroup", "protocol", "principalKind", "person", "credentialId");
    [Fact] void should_name_group() => _properties["endpointGroup"].ShouldEqual("capabilities");
    [Fact] void should_name_protocol() => _properties["protocol"].ShouldEqual("rest");
    [Fact] void should_name_kind() => _properties["principalKind"].ShouldEqual("person");
    [Fact] void should_name_person() => _properties["person"].ShouldEqual("person");
    [Fact] void should_name_credential() => _properties["credentialId"].ShouldEqual("credential");
}
