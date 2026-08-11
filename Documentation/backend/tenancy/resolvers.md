# Resolving Tenant IDs

Arc resolves tenant IDs through pluggable strategies. Each request is evaluated by the configured resolver, and the resulting tenant ID becomes the active tenant context for the request lifecycle.

## Built-In Resolvers

### Header Resolver (Default)

Resolves the tenant ID from an HTTP header.

```csharp
builder.AddCratisArcCore(options =>
{
    options.UseHeaderTenancy("X-Custom-Tenant");
});
```

Default header name: `x-cratis-tenant-id`

### Query Parameter Resolver

Resolves the tenant ID from a query string parameter.

```csharp
builder.AddCratisArcCore(options =>
{
    options.UseQueryTenancy("tenant");
});
```

Default parameter name: `tenantId`

### Claim Resolver

Resolves the tenant ID from a claim on the authenticated user.

```csharp
builder.AddCratisArcCore(options =>
{
    options.UseClaimTenancy("tenant_id");
});
```

Default claim type: `tenant_id`

### Subdomain Resolver

Resolves the tenant ID from the subdomain of a configured base domain, and falls back to the configured HTTP header for every host that does not carry one.

```csharp
builder.AddCratisArcCore(options =>
{
    options.UseSubdomainTenancy("myapp.com", "X-Custom-Tenant");
});
```

A host carries a tenant only when it is **exactly one label in front of the base domain**. A request to `acme.myapp.com` resolves the tenant as `acme`. A request to `myapp.com` falls back to the `X-Custom-Tenant` header. This pattern is useful for SaaS applications where each tenant is routed through its own subdomain.

The number of labels in a host is never used to decide whether it carries a tenant, so the following all fall back to the header:

| Host | Resolved tenant |
|---|---|
| `acme.myapp.com` | `acme` |
| `myapp.com` (the base domain itself) | the header |
| `acme.staging.myapp.com` (more than one label) | the header |
| `10.0.0.5`, `[::1]` (IP literals) | the header |
| `otherapp.com` (an unrelated domain) | the header |
| `evil-myapp.com` (ends with the same text, no label boundary) | the header |
| `acme.myapp.com.evil.com` (base domain in the middle) | the header |
| `*.myapp.com`, `user@acme.myapp.com`, `a_b.myapp.com` (not a DNS label) | the header |

Hosts are normalized before matching, so a trailing dot, a port, mixed casing and an internationalized name all resolve the same tenant: `ACME.MyApp.com.`, `acme.myapp.com:5000` and `acme.myapp.com` all resolve `acme`, and `münchen.myapp.com` resolves the punycode label `xn--mnchen-3ya` — the same tenant as `xn--mnchen-3ya.myapp.com`.

Use the base domain to decide what your application's own host is. If the application is served from `www.myapp.com`, configure that as the base domain and `www.myapp.com` falls back to the header while `acme.www.myapp.com` resolves `acme`. With `myapp.com` as the base domain, `www` is an ordinary tenant label like any other — no host name is treated as special.

Default fallback header: `x-cratis-tenant-id`

#### The base domain is required

The base domain is what separates a tenant host from any other host on the internet, so the resolver refuses to start without a usable one. `UseSubdomainTenancy` throws `BaseDomainIsNotADomainName` when the value is empty, is a single label, is an address literal, or is not made of letter-digit-hyphen labels:

```csharp
options.UseSubdomainTenancy("myapp.com");       // fine
options.UseSubdomainTenancy("X-Tenant-Id");     // throws — a header name is not a domain
options.UseSubdomainTenancy("localhost");       // throws — a single label is not a registrable domain
options.UseSubdomainTenancy("192.168.1.10");    // throws — an address identifies no domain
```

Failing to start is deliberate. Without a base domain no host would ever resolve a tenant, and every request would silently take its tenant from the fallback header instead — which any client can set.

**Configure the registrable domain your application is served from, and nothing broader.** A bare top-level domain such as `com` is refused because it is a single label, but the check cannot know that `co.uk` is a public suffix: with `co.uk` as the base domain, anyone who registers `evil.co.uk` becomes the tenant `evil`. Pick the domain you actually own.

#### The resolved tenant must be a DNS label

The resolved label becomes the Chronicle namespace and part of the database name, so it is required to be a valid letter-digit-hyphen label — up to 63 characters, starting and ending with a letter or digit. Anything else falls back to the header rather than travelling on as a tenant ID.

#### One domain, many spellings

Hosts are matched after IDNA compatibility mapping, which is what lets `münchen.myapp.com` and `xn--mnchen-3ya.myapp.com` be the same tenant. The same mapping means several byte sequences are **the same host**, and all resolve the tenant `admin`:

| Written as | Why it is the same |
|---|---|
| `admin.myapp.com` | the canonical spelling |
| `ａdmin.myapp.com` | fullwidth Latin letters map to ASCII |
| `admin.myapp.com.` | a root-anchored name |
| `admin。myapp.com`, `admin．myapp.com`, `admin｡myapp.com` | U+3002, U+FF0E and U+FF61 are label separators |
| `ad<ZWSP>min.myapp.com` | zero width space, soft hyphen, byte order mark and word joiner are ignorable |

This is IDNA working as specified — browsers resolve these the same way — so Arc does not reject them. Be aware of the consequence: **a WAF, ingress or router that matches the literal `Host` string sees a different value than Arc does.** If something upstream makes decisions per tenant host, normalize the host there too, or make the decision from Arc's resolved tenant rather than from the raw header.

#### The fallback header is client-supplied

Every host that does not carry a tenant falls back to `HttpHeader`, and that header arrives on the request unauthenticated — any caller can set it. **Strip the fallback header at your ingress** so only your own infrastructure can set it, exactly as you would for any other trusted request header.

### Development Resolver

Uses a fixed tenant ID for local development.

```csharp
builder.AddCratisArcCore(options =>
{
    options.UseDevelopmentTenancy("my-test-tenant");
});
```

Default tenant ID: `development`

