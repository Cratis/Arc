// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization.for_AuthorizationPolicyContext;

public class when_comparing_a_pipeline_context_with_a_reconstructed_one : Specification
{
    AuthorizationPolicyContext _pipelineContext;
    AuthorizationPolicyContext _reconstructed;
    AuthorizationPolicyContext _copied;
    AuthorizationPolicyContext _differentTime;

    void Establish()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "alice")], "test"));
        var target = typeof(when_comparing_a_pipeline_context_with_a_reconstructed_one);
        var resource = new object();
        var receivedAt = new DateTimeOffset(2026, 9, 28, 12, 0, 0, TimeSpan.Zero);

        _pipelineContext = new AuthorizationPolicyContext(principal, target, resource)
        {
            ReceivedAt = receivedAt,
            PrincipalAccessor = Substitute.For<ICurrentPrincipalAccessor>()
        };
        _reconstructed = new AuthorizationPolicyContext(principal, target, resource) { ReceivedAt = receivedAt };
        _differentTime = _reconstructed with { ReceivedAt = receivedAt.AddSeconds(1) };
    }

    void Because() => _copied = _pipelineContext with { };

    [Fact] void should_equal_the_reconstructed_context() => _pipelineContext.ShouldEqual(_reconstructed);
    [Fact] void should_share_the_reconstructed_hash_code() => _pipelineContext.GetHashCode().ShouldEqual(_reconstructed.GetHashCode());
    [Fact] void should_equal_a_with_copy() => _copied.ShouldEqual(_pipelineContext);
    [Fact] void should_keep_the_accessor_on_a_with_copy() => ReferenceEquals(_copied.PrincipalAccessor, _pipelineContext.PrincipalAccessor).ShouldBeTrue();
    [Fact] void should_still_distinguish_public_input() => _reconstructed.ShouldNotEqual(_differentTime);
}
