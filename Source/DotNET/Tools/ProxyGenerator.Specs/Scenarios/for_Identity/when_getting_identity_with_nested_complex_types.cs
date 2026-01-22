// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Identity;

/// <summary>
/// Specification for verifying that identity details types with nested complex types
/// generate correct TypeScript code with proper imports and field decorators.
/// </summary>
public class when_generating_types_for_identity_details_with_nested_complex_types : Specification
{
    string _addressTypeScript;
    string _userPreferenceTypeScript;
    string _userRoleTypeScript;
    string _organizationTypeScript;
    string _userIdentityDetailsTypeScript;

    void Establish()
    {
        // Generate TypeScript for all types involved in the identity details
        var addressDescriptor = typeof(Address).ToTypeDescriptor(string.Empty, 0);
        _addressTypeScript = TemplateTypes.Type(addressDescriptor);

        var userPreferenceDescriptor = typeof(UserPreference).ToTypeDescriptor(string.Empty, 0);
        _userPreferenceTypeScript = TemplateTypes.Type(userPreferenceDescriptor);

        var userRoleDescriptor = typeof(UserRole).ToTypeDescriptor(string.Empty, 0);
        _userRoleTypeScript = TemplateTypes.Type(userRoleDescriptor);

        var organizationDescriptor = typeof(Organization).ToTypeDescriptor(string.Empty, 0);
        _organizationTypeScript = TemplateTypes.Type(organizationDescriptor);

        var userIdentityDetailsDescriptor = typeof(UserIdentityDetails).ToTypeDescriptor(string.Empty, 0);
        _userIdentityDetailsTypeScript = TemplateTypes.Type(userIdentityDetailsDescriptor);
    }

    [Fact] void should_generate_address_with_field_decorator_import() => _addressTypeScript.ShouldContain("import { field } from '@cratis/fundamentals';");
    [Fact] void should_generate_address_street_field() => _addressTypeScript.ShouldContain("@field(String)");
    [Fact] void should_generate_address_street_property() => _addressTypeScript.ShouldContain("street!: string;");
    [Fact] void should_generate_address_city_property() => _addressTypeScript.ShouldContain("city!: string;");
    [Fact] void should_generate_address_country_property() => _addressTypeScript.ShouldContain("country!: string;");
    [Fact] void should_generate_address_postal_code_as_string_from_concept() => _addressTypeScript.ShouldContain("postalCode!: string;");
    [Fact] void should_generate_user_preference_class() => _userPreferenceTypeScript.ShouldContain("export class UserPreference");
    [Fact] void should_generate_user_preference_key_field() => _userPreferenceTypeScript.ShouldContain("key!: string;");
    [Fact] void should_generate_user_preference_value_field() => _userPreferenceTypeScript.ShouldContain("value!: string;");
    [Fact] void should_generate_user_role_class() => _userRoleTypeScript.ShouldContain("export class UserRole");
    [Fact] void should_generate_user_role_name_as_string_from_concept() => _userRoleTypeScript.ShouldContain("name!: string;");
    [Fact] void should_generate_user_role_permissions_array() => _userRoleTypeScript.ShouldContain("permissions!: string[];");
    [Fact] void should_generate_organization_class() => _organizationTypeScript.ShouldContain("export class Organization");
    [Fact] void should_generate_organization_id_as_guid_from_concept() => _organizationTypeScript.ShouldContain("id!: Guid;");
    [Fact] void should_generate_organization_address_import() => _organizationTypeScript.ShouldContain("import { Address } from './Address';");
    [Fact] void should_generate_organization_address_field_decorator() => _organizationTypeScript.ShouldContain("@field(Address)");
    [Fact] void should_generate_organization_address_property() => _organizationTypeScript.ShouldContain("address?: Address;");
    [Fact] void should_generate_identity_details_class() => _userIdentityDetailsTypeScript.ShouldContain("export class UserIdentityDetails");
    [Fact] void should_generate_identity_details_address_import() => _userIdentityDetailsTypeScript.ShouldContain("import { Address } from './Address';");
    [Fact] void should_generate_identity_details_user_preference_import() => _userIdentityDetailsTypeScript.ShouldContain("import { UserPreference } from './UserPreference';");
    [Fact] void should_generate_identity_details_user_role_import() => _userIdentityDetailsTypeScript.ShouldContain("import { UserRole } from './UserRole';");
    [Fact] void should_generate_identity_details_organization_import() => _userIdentityDetailsTypeScript.ShouldContain("import { Organization } from './Organization';");
    [Fact] void should_generate_identity_details_home_address_field() => _userIdentityDetailsTypeScript.ShouldContain("@field(Address)");
    [Fact] void should_generate_identity_details_home_address_property() => _userIdentityDetailsTypeScript.ShouldContain("homeAddress?: Address;");
    [Fact] void should_generate_identity_details_preferences_as_array() => _userIdentityDetailsTypeScript.ShouldContain("@field(UserPreference, true)");
    [Fact] void should_generate_identity_details_preferences_property() => _userIdentityDetailsTypeScript.ShouldContain("preferences!: UserPreference[];");
    [Fact] void should_generate_identity_details_roles_as_array() => _userIdentityDetailsTypeScript.ShouldContain("@field(UserRole, true)");
    [Fact] void should_generate_identity_details_organization_field() => _userIdentityDetailsTypeScript.ShouldContain("@field(Organization)");
}
