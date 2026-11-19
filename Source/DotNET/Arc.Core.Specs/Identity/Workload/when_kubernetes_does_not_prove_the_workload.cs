// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json.Nodes;
using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Identity.Workload;

public class when_kubernetes_does_not_prove_the_workload : Specification
{
    readonly Dictionary<string, CredentialVerification> _results = [];
    readonly Dictionary<string, int> _reviews = [];
    readonly Dictionary<string, Action<given.workload_verification>> _scenarios = new()
    {
        ["unauthenticated"] = fixture => fixture.Handler.Review["status"]!["authenticated"] = false,
        ["wrong-audience"] = fixture => fixture.Handler.Review["status"]!["audiences"] = new JsonArray("other-audience"),
        ["review-error"] = fixture => fixture.Handler.Review["status"]!["error"] = "synthetic failure",
        ["wrong-username"] = fixture => fixture.Handler.Review["status"]!["user"]!["username"] = "system:node:fixture",
        ["missing-groups"] = fixture => fixture.Handler.Review["status"]!["user"]!["groups"] = new JsonArray("system:authenticated"),
        ["unknown-service-account-uid"] = fixture => fixture.Handler.Review["status"]!["user"]!["uid"] = "recreated-sa-uid",
        ["unknown-namespace"] = fixture => fixture.Handler.Review["status"]!["user"]!["username"] = "system:serviceaccount:other:worker",
        ["unbound-token"] = fixture => fixture.Handler.Review["status"]!["user"]!.AsObject().Remove("extra"),
        ["missing-pod-uid"] = fixture => fixture.Handler.Review["status"]!["user"]!["extra"]!.AsObject().Remove("authentication.kubernetes.io/pod-uid"),
        ["ambiguous-pod-binding"] = fixture => fixture.Handler.Review["status"]!["user"]!["extra"]!["authentication.kubernetes.io/pod-uid"] = new JsonArray("pod-uid", "another-uid"),
        ["recreated-pod"] = fixture => fixture.Handler.Pod["metadata"]!["uid"] = "recreated-pod-uid",
        ["deleting-pod"] = fixture => fixture.Handler.Pod["metadata"]!["deletionTimestamp"] = "2026-01-01T00:00:00Z",
        ["wrong-pod-service-account"] = fixture => fixture.Handler.Pod["spec"]!["serviceAccountName"] = "other-worker",
        ["wrong-pod-namespace"] = fixture => fixture.Handler.Pod["metadata"]!["namespace"] = "other",
        ["wrong-pod-name"] = fixture => fixture.Handler.Pod["metadata"]!["name"] = "other",
        ["malformed-json"] = fixture => fixture.Handler.InvalidJson = true,
        ["unavailable"] = fixture => fixture.Handler.Unavailable = true,
        ["pod-missing"] = fixture => fixture.Handler.PodStatus = HttpStatusCode.NotFound,
        ["pod-rbac-denied"] = fixture => fixture.Handler.PodStatus = HttpStatusCode.Forbidden,
        ["pod-outage"] = fixture => fixture.Handler.PodUnavailable = true,
        ["reviewer-rbac-denied"] = fixture => fixture.Handler.Status = HttpStatusCode.Forbidden,
        ["redirect"] = fixture => fixture.Handler.Status = HttpStatusCode.TemporaryRedirect,
        ["unreadable-reviewer"] = fixture => fixture.Reviewer = ""
    };

    async Task Because()
    {
        foreach (var (name, configure) in _scenarios)
        {
            using var fixture = new given.workload_verification();
            configure(fixture);
            _results[name] = await fixture.Verify();
            await fixture.Verify();
            _reviews[name] = fixture.Handler.Reviews;
        }
    }

    [Fact] void should_execute_every_scenario() => _results.Keys.ShouldContainOnly(_scenarios.Keys);
    [Fact] void should_refuse_every_unproven_identity() => _results.Values.All(result => result.Refusal == CallerRefusal.SourceRefused).ShouldBeTrue();
    [Fact] void should_not_expose_partial_authority() => _results.Values.All(result => result.Facts is null).ShouldBeTrue();
    [Fact] void should_not_cache_rejections() => _reviews.Where(pair => pair.Key != "unreadable-reviewer").All(pair => pair.Value == 2).ShouldBeTrue();
    [Fact] void should_not_call_the_api_without_a_reviewer() => _reviews["unreadable-reviewer"].ShouldEqual(0);
}
