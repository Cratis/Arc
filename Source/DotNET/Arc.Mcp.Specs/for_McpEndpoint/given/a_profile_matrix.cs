// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Mcp.for_McpEndpoint.given;

/// <summary>
/// Calls a write tool once per credential kind on a host with one profile, recording what was admitted and what ran.
/// </summary>
public abstract class a_profile_matrix : an_mcp_host
{
    protected Dictionary<CallerCredentialKind, McpResponse> _responses;
    protected Dictionary<CallerCredentialKind, int> _dispatched;

    protected abstract IReadOnlySet<CallerCredentialKind> Accepted { get; }

    protected IEnumerable<CallerCredentialKind> Kinds => Enum.GetValues<CallerCredentialKind>();

    protected async Task CallEveryKind()
    {
        _responses = [];
        _dispatched = [];
        foreach (var kind in Kinds)
        {
            AuthenticateIn("tenant-a", kind, ReadScope, WriteScope);
            var before = _probe.Count;
            _responses[kind] = await CallTool("write_note", """{"text":"x"}""");
            _dispatched[kind] = _probe.Count - before;
        }
    }

    protected bool AdmittedKinds() => Kinds.Where(Accepted.Contains).All(_ => _responses[_].Status == HttpStatusCode.OK && _dispatched[_] == 1);

    protected bool RefusedKinds() => Kinds.Where(_ => !Accepted.Contains(_)).All(_ =>
        _responses[_].Status == HttpStatusCode.Unauthorized && _responses[_].RefusalError == "unauthorized" && _dispatched[_] == 0);

    protected bool CheckedEveryKind() => _responses.Count == Enum.GetValues<CallerCredentialKind>().Length;
}
