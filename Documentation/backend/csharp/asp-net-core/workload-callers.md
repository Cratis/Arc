# Kubernetes workload callers

Arc can verify a Kubernetes projected service-account token on a capability port and expose a tenantless `cluster-workload` caller through the released, scoped `IVerifiedCallerAccessor`. This is not a person, organization automation, delegation grant, or operation authorization.

## Configure the receiving service

```csharp
using Cratis.Arc;
using Cratis.Arc.Capabilities;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Identity.Workload;

builder.AddCratisArc(
    options => options.EndpointGroups.Assign("capabilities", 8081),
    configureBuilder: arc => arc
        .WithCapabilities(capabilities =>
        {
            capabilities.AllowedPrincipalKinds.Clear();
            capabilities.AllowedPrincipalKinds.Add(CapabilityPrincipalKind.ClusterWorkload);
        })
        .WithVerifiedCallers(callers => callers
            .ForResource("https://jobs.example.test", "jobs-api", "test")
            .UseWorkloadCallers(workload => workload.WithOptions(new WorkloadCallerOptions
            {
                ApiServer = new Uri("https://kubernetes.default.svc"),
                ReviewerTokenPath = "/var/run/secrets/kubernetes.io/serviceaccount/token",
                CaPath = "/var/run/secrets/kubernetes.io/serviceaccount/ca.crt",
                DeletedPodRejectionLatency = TimeSpan.FromSeconds(30),
                CacheCapacity = 1024,
                RequestTimeout = TimeSpan.FromSeconds(5),
                Allowlist = new ServicePrincipalAllowlist(
                [
                    new ServicePrincipalAllowlistEntry(
                        "jobs", "worker", "replace-with-service-account-uid",
                        "https://jobs.example.test", "test",
                        new CallerServicePrincipalId("jobs-worker"),
                        new HashSet<string> { "jobs.read" },
                        new HashSet<string> { "jobs.run" })
                ])
            }))));
```

Configure the hosting listener for the capability port as usual. `UseWorkloadCallers` requires capability hosting and an exact resource, audience and environment. It does not require a person directory and cannot be combined with signed-browser or PAT installation in this initial workload profile. Invalid bounds, ambiguous allowlists, missing mount paths, and invalid CA material fail installation or startup. The allowlist and its scope/permission sets are snapshotted; recreating a service account requires a deliberate UID mapping update.

A command, query or tool can inject `IVerifiedCallerAccessor` and await `GetCurrent()`. A refusal supplies no caller. Apply product authorization against the verified service principal and its scope and permission ceilings; a valid token is not permission to perform an operation.

## Project the sending Pod's token

The sender must use a Pod-bound projected token with the receiver's exact audience, not a legacy Secret-based or unbound service-account token:

```yaml
apiVersion: v1
kind: Pod
metadata:
  name: jobs-worker
  namespace: jobs
spec:
  serviceAccountName: worker
  automountServiceAccountToken: false
  containers:
    - name: worker
      image: example.invalid/worker:fixture
      volumeMounts:
        - name: receiver-token
          mountPath: /var/run/receiver
          readOnly: true
  volumes:
    - name: receiver-token
      projected:
        sources:
          - serviceAccountToken:
              path: token
              audience: jobs-api
              expirationSeconds: 600
```

Read that mounted file as it rotates and send its value in exactly one `Authorization: Bearer` header. Never send it in a URI, cookie, form, log, or forwarded-user header. The receiving service uses a **different reviewer token** accepted by the Kubernetes API, not this receiver-audience token. Its reviewer credential is reread for every API call to support mounted-token rotation.

## Kubernetes trust and reviewer RBAC

The receiver calls `authentication.k8s.io/v1` TokenReview with `spec.audiences` containing only its configured receiving audience. It requires `status.authenticated`, a matching `status.audiences`, canonical `system:serviceaccount:<namespace>:<name>` identity and service-account groups, the service-account UID, and single-valued Pod name/UID extras. Missing, malformed, unauthenticated, wrong-audience, or unbound responses refuse.

The reviewer needs only:

- `create` on `tokenreviews` in `authentication.k8s.io` (a ClusterRole bound to the receiver's reviewer service account).
- `get` on `pods` in the core API, through a namespaced Role/RoleBinding in each allowed sender namespace. No Pod `list`, `watch`, or write permissions are needed.

Arc GETs that named Pod and checks its namespace, name, immutable UID and `spec.serviceAccountName`, rejecting missing or deleting Pods. A recreated Pod with the same name has a different UID and is refused. RBAC denial, API timeout/outage, cancellation, malformed JSON, and unreadable reviewer credentials fail closed without a partial caller or weaker-verifier fallback. The API connection is HTTPS-only, trusts only the configured CA, rejects hostname mismatch, and follows neither redirects nor inherited proxies. Kubernetes network policy should restrict this receiver to its configured API server.

Positive results are cached in a bounded, installation-local cache using a SHA-256 token digest and the exact audience/resource/environment, retaining the verified Pod UID. No raw token is retained as a cache key or identifier. Lifetime is at most the lesser of token expiry and `DeletedPodRejectionLatency`, measured from verification start. Cache hits do not extend it; rejection and cancellation are not cached. Deleting a Pod may therefore leave already cached authority usable for up to that declared latency, never indefinitely. After expiry an unavailable API refuses instead of using stale authority. The default and maximum latency are 30 seconds; configurable latency must be greater than zero and at most 30 seconds. A larger value is rejected during installation.

## Lane and authority boundaries

Only the server-owned capability lane selects the workload verifier. Client headers, JWT payload inspection, and failed OAuth/PAT attempts cannot select it. The existing credential-cardinality gate runs first: duplicates, credential-bearing query strings, mixed session cookies, legacy namespaces and unsupported DPoP presentations retain their existing refusals. The endpoint routing hint participates in request-cache mutation detection.

The workload principal is rebuilt from verified service-account facts. Unrelated `HttpContext.User`, unsigned user/email/role/tenant/workload headers, person assurance and grants do not become authority. Workload verification and capability admission do not run person tenant-membership checks or person claims transformations. The gate refuses workload requests with HTTP 403 when a tenant is supplied through the configured tenant header, query parameter or claim, or selected by the tenant resolver or an existing tenant scope. No handler is invoked. Admitted workload requests run with the downstream tenant accessor pinned to `TenantId.NotSet`; the caller's tenant remains absent. An overridden tenant accessor that Arc cannot pin is refused. Default-port and non-capability behavior is unchanged. mTLS/SPIFFE sender binding is not added by this profile; any configured transport admission remains a separate requirement.

## Boundary coverage, not full conformance

This profile implements the Kubernetes workload boundary described in the locked Arc verified-caller specification (`arc-verified-caller-v1`). Local specs exercise TokenReview/Pod parsing, mapping, cache bounds and a real capability host, but are not a full Identity conformance runner or a live Kubernetes integration test.

The [verified-caller construction checks](verified-callers.md) include row 18's missing verified grant. Row 26 (a request asking for grant-b while the credential maps to grant-a) remains deferred to the future grant-lookup verifier; this workload profile never reads a request grant or produces person delegation. There is no claimed OAuth grant, DPoP, approval or runner adapter here.
