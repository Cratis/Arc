// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;
using Cratis.Concepts;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Identity;

/// <summary>
/// Represents a postal code concept.
/// </summary>
/// <param name="Value">The postal code value.</param>
public record PostalCode(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents an empty postal code.
    /// </summary>
    public static readonly PostalCode Empty = new(string.Empty);

    /// <summary>
    /// Implicitly converts a string to a postal code.
    /// </summary>
    /// <param name="value">The string value.</param>
    public static implicit operator PostalCode(string value) => new(value);
}

/// <summary>
/// Represents an organization identifier concept.
/// </summary>
/// <param name="Value">The organization identifier value.</param>
public record OrganizationId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Represents an unset organization identifier.
    /// </summary>
    public static readonly OrganizationId NotSet = new(Guid.Empty);

    /// <summary>
    /// Implicitly converts a Guid to an organization identifier.
    /// </summary>
    /// <param name="value">The Guid value.</param>
    public static implicit operator OrganizationId(Guid value) => new(value);

    /// <summary>
    /// Creates a new unique organization identifier.
    /// </summary>
    /// <returns>A new <see cref="OrganizationId"/>.</returns>
    public static OrganizationId New() => new(Guid.NewGuid());
}

/// <summary>
/// Represents a role name concept.
/// </summary>
/// <param name="Value">The role name value.</param>
public record RoleName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents an empty role name.
    /// </summary>
    public static readonly RoleName Empty = new(string.Empty);

    /// <summary>
    /// Implicitly converts a string to a role name.
    /// </summary>
    /// <param name="value">The string value.</param>
    public static implicit operator RoleName(string value) => new(value);
}

/// <summary>
/// Represents a nested address type.
/// </summary>
public class Address
{
    /// <summary>
    /// Gets or sets the street.
    /// </summary>
    public string Street { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the city.
    /// </summary>
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the country.
    /// </summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the postal code.
    /// </summary>
    public PostalCode PostalCode { get; set; } = PostalCode.Empty;
}

/// <summary>
/// Represents a user preference setting.
/// </summary>
public class UserPreference
{
    /// <summary>
    /// Gets or sets the preference key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the preference value.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the category.
    /// </summary>
    public string Category { get; set; } = string.Empty;
}

/// <summary>
/// Represents a user role with permissions.
/// </summary>
public class UserRole
{
    /// <summary>
    /// Gets or sets the role name.
    /// </summary>
    public RoleName Name { get; set; } = RoleName.Empty;

    /// <summary>
    /// Gets or sets the permissions.
    /// </summary>
    public IEnumerable<string> Permissions { get; set; } = [];
}

/// <summary>
/// Represents organization information.
/// </summary>
public class Organization
{
    /// <summary>
    /// Gets or sets the organization ID.
    /// </summary>
    public OrganizationId Id { get; set; } = OrganizationId.NotSet;

    /// <summary>
    /// Gets or sets the organization name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the organization address.
    /// </summary>
    public Address? Address { get; set; }
}

/// <summary>
/// Represents the identity details with nested complex types.
/// </summary>
public class UserIdentityDetails
{
    /// <summary>
    /// Gets or sets the user's department.
    /// </summary>
    public string Department { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's job title.
    /// </summary>
    public string JobTitle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's home address.
    /// </summary>
    public Address? HomeAddress { get; set; }

    /// <summary>
    /// Gets or sets the user's work address.
    /// </summary>
    public Address? WorkAddress { get; set; }

    /// <summary>
    /// Gets or sets the user's preferences.
    /// </summary>
    public IEnumerable<UserPreference> Preferences { get; set; } = [];

    /// <summary>
    /// Gets or sets the user's roles.
    /// </summary>
    public IEnumerable<UserRole> Roles { get; set; } = [];

    /// <summary>
    /// Gets or sets the user's organization.
    /// </summary>
    public Organization? Organization { get; set; }
}

/// <summary>
/// Provides identity details with nested complex types for testing.
/// </summary>
public class UserIdentityDetailsProvider : IProvideIdentityDetails<UserIdentityDetails>
{
    /// <summary>
    /// Gets the test details to return.
    /// </summary>
    public static UserIdentityDetails TestDetails { get; set; } = new()
    {
        Department = "Engineering",
        JobTitle = "Software Developer",
        HomeAddress = new Address
        {
            Street = "123 Home Street",
            City = "Home City",
            Country = "Home Country",
            PostalCode = "12345"
        },
        WorkAddress = new Address
        {
            Street = "456 Work Avenue",
            City = "Work City",
            Country = "Work Country",
            PostalCode = "67890"
        },
        Preferences =
        [
            new UserPreference { Key = "theme", Value = "dark", Category = "ui" },
            new UserPreference { Key = "language", Value = "en", Category = "localization" }
        ],
        Roles =
        [
            new UserRole { Name = "Admin", Permissions = ["read", "write", "delete"] },
            new UserRole { Name = "Developer", Permissions = ["read", "write"] }
        ],
        Organization = new Organization
        {
            Id = OrganizationId.New(),
            Name = "Cratis",
            Address = new Address
            {
                Street = "789 Corp Blvd",
                City = "Corp City",
                Country = "Corp Country",
                PostalCode = "11111"
            }
        }
    };

    /// <inheritdoc/>
    public Task<IdentityDetails> Provide(IdentityProviderContext context)
    {
        return Task.FromResult(new IdentityDetails(true, TestDetails));
    }
}
