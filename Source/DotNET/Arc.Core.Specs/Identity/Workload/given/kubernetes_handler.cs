// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;
using System.Text.Json.Nodes;

namespace Cratis.Arc.Identity.Workload.given;

internal sealed class kubernetes_handler : HttpMessageHandler
{
    internal JsonNode Review = JsonNode.Parse("""{"status":{"authenticated":true,"audiences":["workload-audience"],"user":{"username":"system:serviceaccount:jobs:worker","uid":"sa-uid","groups":["system:authenticated","system:serviceaccounts","system:serviceaccounts:jobs"],"extra":{"authentication.kubernetes.io/pod-name":["pod-fixture"],"authentication.kubernetes.io/pod-uid":["pod-uid"]}}}}""")!;
    internal JsonNode Pod = JsonNode.Parse("""
        {"metadata":{"name":"pod-fixture","namespace":"jobs","uid":"pod-uid"},"spec":{"serviceAccountName":"worker"}}
        """)!;
    internal HttpStatusCode Status = HttpStatusCode.OK;
    internal bool Unavailable;
    internal bool PodUnavailable;
    internal HttpStatusCode PodStatus = HttpStatusCode.OK;
    internal bool InvalidJson;
    internal int Reviews;
    internal int Pods;
    internal string? SentAudience;
    internal readonly List<string?> ReviewerCredentials = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ReviewerCredentials.Add(request.Headers.Authorization?.Parameter);
        var post = request.Method == HttpMethod.Post;
        if (post)
        {
            Reviews++;
            var sent = JsonNode.Parse(await request.Content!.ReadAsStringAsync(cancellationToken))!;
            SentAudience = sent["spec"]!["audiences"]![0]!.GetValue<string>();
        }
        else
        {
            Pods++;
        }
        if (Unavailable || (!post && PodUnavailable)) throw new HttpRequestException("Synthetic Kubernetes outage.");
        return new(post ? Status : PodStatus) { Content = new StringContent(InvalidJson ? "not-json" : (post ? Review : Pod).ToJsonString(), Encoding.UTF8, "application/json") };
    }
}
