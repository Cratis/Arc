// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Claims;

namespace Cratis.Arc.Authorization.for_StateAwareAuthorizationPolicy.given;

public class a_plan_policy : Specification
{
    protected PlanPolicy _policy;
    protected bool _allowed;
    protected AuthorizationPolicyContext _context;

    void Establish()
    {
        _policy = new PlanPolicy();
        _context = new AuthorizationPolicyContext(new ClaimsPrincipal(new ClaimsIdentity("test")), typeof(PlanCommand), new PlanCommand("vessel-1"));
    }

    protected Task Authorize() => Run(_context);

    protected async Task Run(AuthorizationPolicyContext context) => _allowed = await _policy.IsAuthorized(context, CancellationToken.None);

    public record PlanCommand(string AssetId);

    public record AssetPlan(bool DataQuality);

    public class PlanPolicy : StateAwareAuthorizationPolicy<AssetPlan>
    {
        public Dictionary<string, AssetPlan> Plans { get; } = [];

        public Exception? ReadFailure { get; set; }

        public List<StateDenialReason> Denials { get; } = [];

        protected override object? GetKey(AuthorizationPolicyContext context) => (context.Resource as PlanCommand)?.AssetId;

        protected override ValueTask<AssetPlan?> Read(object key, CancellationToken cancellationToken) =>
            ReadFailure is not null ? throw ReadFailure : ValueTask.FromResult(Plans.GetValueOrDefault((string)key));

        protected override bool Decide(ClaimsPrincipal principal, AssetPlan state) => state.DataQuality;

        protected override void OnDenied(AuthorizationPolicyContext context, StateDenialReason reason, Exception? exception) => Denials.Add(reason);
    }
}
