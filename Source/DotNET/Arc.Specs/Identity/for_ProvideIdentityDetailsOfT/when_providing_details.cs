// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_ProvideIdentityDetailsOfT;

public class when_providing_details : Specification
{
    record MyDetails(string Name, int Age);

    class MyTypedProvider : IProvideIdentityDetails<MyDetails>
    {
        public Task<IdentityDetails<MyDetails>> ProvideDetails(IdentityProviderContext context)
        {
            return Task.FromResult(new IdentityDetails<MyDetails>(true, new MyDetails("John", 30)));
        }
    }

    MyTypedProvider _provider;
    IdentityProviderContext _context;
    IdentityDetails _result;

    void Establish()
    {
        _provider = new MyTypedProvider();
        _context = new IdentityProviderContext(
            IdentityId.Empty,
            IdentityName.Empty,
            []);
    }

    async Task Because() => _result = await ((IProvideIdentityDetails)_provider).Provide(_context);

    [Fact] void should_indicate_user_is_authorized() => _result.IsUserAuthorized.ShouldBeTrue();
    [Fact] void should_contain_typed_details() => _result.Details.ShouldBeOfExactType<MyDetails>();
    [Fact] void should_have_correct_name() => ((MyDetails)_result.Details).Name.ShouldEqual("John");
    [Fact] void should_have_correct_age() => ((MyDetails)_result.Details).Age.ShouldEqual(30);
}
