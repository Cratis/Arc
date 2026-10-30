// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Reflection;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Http.for_HttpListenerEndpointMapper;

public class when_tenant_membership_is_denied : given.a_running_endpoint_mapper
{
    HttpResponseMessage _response;
    string _body;
    void Establish()
    {
        _endpointMapper.MapGet("/denied", _ => throw new TargetInvocationException(new TenantMembershipDenied()));
        StartEndpointMapper();
    }
    async Task Because()
    {
        _response = await _httpClient.GetAsync("/denied", TestContext.Current.CancellationToken);
        _body = await _response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }
    void Destroy() => _response?.Dispose();
    [Fact] void should_respond_forbidden() => _response.StatusCode.ShouldEqual(HttpStatusCode.Forbidden);
    [Fact] void should_not_return_details() => _body.ShouldEqual(string.Empty);
}
