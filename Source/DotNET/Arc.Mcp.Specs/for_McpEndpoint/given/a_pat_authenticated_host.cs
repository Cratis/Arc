// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.PersonalAccessTokens;
using Cratis.Arc.Tenancy;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Mcp.for_McpEndpoint.given;

public class a_pat_authenticated_host : an_mcp_host
{
    protected const string SessionId = "45f649e7-1bfd-405e-9b21-895894437ac0";
    protected string _pat;
    protected PatRecord _record;
    protected bool _policyAllows = true;
    protected int _authorityReads;
    protected int _policyChecks;
    protected McpResponse _initialized;

    protected override void ConfigureArcOptions(ArcOptions options)
    {
        options.Tenancy.ResolverType = TenantResolverType.Fixed;
        options.Tenancy.FixedTenantId = "tenant-a";
    }

    protected override void ConfigureCallers(IServiceCollection services)
    {
        var arc = Substitute.For<IArcBuilder>();
        arc.Services.Returns(services);
        arc.WithPersonalAccessTokens(new("direct") { AllowedScopes = [ReadScope, WriteScope] }, callers => callers
            .ForResource("resource", "audience", "test")
            .UsePersonDirectory<PersonDirectory>());

        // Synthetic bearer generated only in memory; seed the authoritative adapter using the public format contract.
        _pat = SyntheticPat();
        var digest = Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes("cratis-arc-pat-verifier-v1\0" + _pat)));
        var now = DateTimeOffset.UtcNow;
        _record = new(
            new("credential"),
            digest,
            "direct",
            "resource",
            "audience",
            "test",
            new("person"),
            new("https://identity.example", "62a07b13-0838-477d-9f43-1aa3d2911104"),
            new("tenant-a"),
            null,
            new HashSet<string> { ReadScope, WriteScope },
            now.AddMinutes(-1),
            now.AddHours(1),
            new("https://identity.example", now.AddMinutes(-2).ToUnixTimeSeconds(), null, ["sso"]),
            1,
            1);

        var authority = Substitute.For<IPatAuthority>();
        authority.ReadAuthoritative(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _authorityReads++;
            return call.ArgAt<string>(1) == _record.Verifier ? _record : null;
        });
        var epochs = Substitute.For<IPatRecoveryEpoch>();
        epochs.Read(Arg.Any<CancellationToken>()).Returns(1UL);
        var policy = Substitute.For<IPatPolicy>();
        policy.Evaluate(Arg.Any<PatRecord>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _policyChecks++;
            return new PatPolicyDecision(_policyAllows, TimeSpan.FromDays(1), 1);
        });
        services.AddSingleton(authority);
        services.AddSingleton(epochs);
        services.AddSingleton(policy);
    }

    protected async Task Initialize() => _initialized = await Post(
        """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-03-26","capabilities":{},"clientInfo":{"name":"synthetic-client","version":"1"}}}""",
        customize: request => request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + _pat));

    protected Task<McpResponse> SessionCall(string? credential, bool withSession = true) => Post(
        """{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"write_note","arguments":{"text":"synthetic note"}}}""",
        customize: request =>
        {
            if (withSession) request.Headers.TryAddWithoutValidation("Mcp-Session-Id", SessionId);
            if (credential is not null) request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + credential);
        });

    protected static string SyntheticPat()
    {
        var body = "direct_pat_v1_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        return body + "_" + Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(body))).ToLowerInvariant()[..8];
    }
}
