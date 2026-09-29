// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Arc.Identity.for_IdentityProvider.when_getting_typed_details;

public class and_identity_cookie_exists : given.an_identity_provider_result_handler
{
    IdentityProviderResult<given.ProfileDetails> _result;

    void Establish()
    {
        var cookieJson = JsonSerializer.Serialize(given.representative_identity.Result, given.representative_identity.OptionsAsBefore(_options));
        _httpRequestContext.Cookies.Returns(new Dictionary<string, string>
        {
            [IdentityProvider.IdentityCookieName] = Convert.ToBase64String(Encoding.UTF8.GetBytes(cookieJson)),
        });
    }

    async Task Because() => _result = await _handler.Get<given.ProfileDetails>();

    [Fact] void should_read_the_identity() => _result.Id.ShouldEqual(given.representative_identity.Result.Id);
    [Fact] void should_read_the_name() => _result.Name.ShouldEqual(given.representative_identity.Result.Name);
    [Fact] void should_read_the_roles() => _result.Roles.ShouldContainOnly("Admin", "Reader");
    [Fact] void should_read_the_department() => _result.Details.Department.ShouldEqual(given.representative_identity.Details.Department);
    [Fact] void should_read_the_level_concept() => _result.Details.Level.ShouldEqual(given.representative_identity.Details.Level);
    [Fact] void should_read_the_enum() => _result.Details.Access.ShouldEqual(given.AccessKind.Administrator);
    [Fact] void should_read_the_nested_object() => _result.Details.Address.ShouldEqual(given.representative_identity.Details.Address);
    [Fact] void should_read_the_groups() => _result.Details.Groups.ShouldContainOnly("Reader", "Writer");
}
