// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Identity;

internal static partial class ForwardedIdentityPolicyLogging
{
    [LoggerMessage(LogLevel.Warning, "Unsigned forwarded identity is enabled for LocalDevelopment only. Never expose this profile on a deployed host. Configure signed forwarding before deployment.")]
    internal static partial void UnsignedLocalDevelopment(this ILogger logger);

    [LoggerMessage(LogLevel.Warning, "Signed forwarded identity is enabled without required transport certificates. Restrict backend access to the forwarding peer or configure ForwardedIdentity:ClientCertificate:Required=true.")]
    internal static partial void SignedForwardingWithoutCertificate(this ILogger logger);

    [LoggerMessage(LogLevel.Warning, "Forwarded identity v2 is configured without required transport certificates in LocalDevelopment. Requests still require authenticated transport; this profile does not synthesize hop evidence.")]
    internal static partial void V2WithoutCertificateLocalDevelopment(this ILogger logger);

    [LoggerMessage(LogLevel.Warning, "Forwarded identity signing or certificate settings are configured but TrustForwardedIdentityHeaders is false; all forwarded identity forms are disabled.")]
    internal static partial void ForwardingDisabledByMasterSwitch(this ILogger logger);

    [LoggerMessage(LogLevel.Information, "Forwarded identity policy: flow {Flow}, profile {Profile}, unsigned-v1 {UnsignedV1}, signed-v1 {SignedV1}, v2 {V2}, certificate-required {CertificateRequired}")]
    internal static partial void ForwardingPolicyAtStartup(this ILogger logger, string flow, DeploymentProfile profile, bool unsignedV1, bool signedV1, bool v2, bool certificateRequired);

    [LoggerMessage(LogLevel.Information, "Forwarded identity accepted: flow {Flow}, form {Form}; first acceptance for this process")]
    internal static partial void ForwardingAccepted(this ILogger logger, string flow, string form);

    [LoggerMessage(LogLevel.Warning, "Forwarded identity refused: flow {Flow}, reason {Reason}")]
    internal static partial void ForwardingRefused(this ILogger logger, string flow, string reason);
}
