# Verified caller acceptance specs

These executable specs reference the sample host, not Arc internals. One shared Kestrel host receives real HTTP requests on separate capability and default listeners. Every rejection also checks that the publish dispatch count did not change.

`given/TestSigner.cs` implements the documented v2 UTF-8 length framing and P-256 P1363 signing without calling Arc's canonicalizer or verifier. Its sample-local vector subset contains only the first **synthetic test key** and `valid-v2` case from the shared fixture. It uses those inputs, adapting the credential metadata to a browser session and signing a fresh timestamp and nonce. No bearer verifier is installed.

Run from the repository root:

```sh
dotnet test Samples/VerifiedCallerAcceptance.Specs/VerifiedCallerAcceptance.Specs.csproj -c Release
```

The project is compiled by `Arc.slnx` and executed in CI's `specs-3` shard. The sample-local vector subset is copied only to the test output; the production host has no reference to it. Its publicly committed test key material must not be reused for any live environment.
