// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Arc.Identity.Workload;

/// <summary>Configures Arc's concrete Kubernetes workload verifier, without exposing a custom-verifier extension point.</summary>
public sealed class WorkloadCallersBuilder
{
    WorkloadCallerOptions _options = new();

    /// <summary>Sets explicit Kubernetes trust, identity mapping and bounded-cache settings.</summary>
    /// <param name="options">The immutable installation options.</param>
    /// <returns>This builder.</returns>
    public WorkloadCallersBuilder WithOptions(WorkloadCallerOptions options)
    {
        _options = options;
        return this;
    }

    /// <summary>Validates settings and required mounts before registering the installation.</summary>
    /// <exception cref="InvalidVerifiedCallerInstallation">Settings or required mounted files are missing or invalid.</exception>
    internal void Validate()
    {
        if (!_options.IsValid) throw new InvalidVerifiedCallerInstallation("Workload callers require HTTPS Kubernetes trust, a nonempty allowlist and bounded positive-cache settings.");
        if (!File.Exists(_options.ReviewerTokenPath) || !File.Exists(_options.CaPath))
        {
            throw new InvalidVerifiedCallerInstallation("Workload callers require mounted reviewer credentials and a Kubernetes CA.");
        }
    }

    /// <summary>Registers a snapshot of the concrete workload verification pipeline.</summary>
    /// <param name="services">The receiving service collection.</param>
    /// <param name="target">The exact receiving target.</param>
    internal void Install(IServiceCollection services, CallerCredentialTarget target)
    {
        var options = _options;
        services.AddSingleton(new WorkloadCallerInstallation());
        services.AddSingleton(options);
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<WorkloadVerificationCache>();
        services.AddSingleton<IWorkloadTokenReviewClient>(_ => new WorkloadTokenReviewClient(options));
        services.AddScoped<IVerifiedCredentialSource>(provider => new WorkloadCredentialSource(
            provider.GetRequiredService<IWorkloadTokenReviewClient>(),
            options,
            target,
            provider.GetRequiredService<WorkloadVerificationCache>(),
            provider.GetRequiredService<TimeProvider>()));
        services.AddHostedService<WorkloadCallerStartupValidation>();
    }
}
