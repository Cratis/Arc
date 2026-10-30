// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Tenancy.for_TenantIdAccessor.when_getting_current;

public class and_the_principal_changes : given.a_verified_tenant_id_accessor
{
    ClaimsPrincipal _selected;
    void Because()
    {
        _ = _accessor.Current;
        _selected = new ClaimsPrincipal(new ClaimsIdentity("selected-scheme"));
        using var principal = _principals.UseAuthorizationPrincipal(_selected);
        _ = _accessor.Current;
    }
    [Fact] void should_check_the_selected_principal_again() => _membership.Received(1).IsMember(_selected, new TenantId("selected"));
    [Fact] void should_restore_the_request_principal() => _principals.Current.ShouldBeSame(_principal);
}
