// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// Native Arc policies registered through the host-neutral authorization API.
/// </summary>
public static class ArcPolicySource
{
    /// <summary>
    /// Policies whose implementations either state a pure principal condition or explicitly cannot be recovered.
    /// </summary>
    public const string Policies = """
        using System;
        using System.Threading;
        using System.Threading.Tasks;
        using Cratis.Arc.Authorization;

        namespace Library;

        public sealed class AccountantOnlyPolicy : global::Cratis.Arc.Authorization.IAuthorizationPolicy
        {
            public global::System.Threading.Tasks.ValueTask<bool> IsAuthorized(global::Cratis.Arc.Authorization.AuthorizationPolicyContext context, global::System.Threading.CancellationToken cancellationToken)
                => global::System.Threading.Tasks.ValueTask.FromResult((context.Principal.Identity?.IsAuthenticated == true && context.Principal.IsInRole("Accountant")));
        }

        public sealed class CanCompletePolicy : IAuthorizationPolicy
        {
            public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
                => ValueTask.FromResult(context.Principal.IsInRole("Automation"));
        }

        public sealed class NestedPolicy : IAuthorizationPolicy
        {
            public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
                => ValueTask.FromResult((context.Principal.IsInRole("A") || context.Principal.IsInRole("B")) && context.Principal.HasClaim("branch", "central"));
        }

        public sealed class BlockPolicy : IAuthorizationPolicy
        {
            public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return ValueTask.FromResult(context.Principal.IsInRole("Clerk"));
            }
        }

        public sealed class ConstructedPolicy : IAuthorizationPolicy
        {
            public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
                => new ValueTask<bool>(context.Principal.IsInRole("Auditor"));
        }

        public sealed class ExplicitPolicy : IAuthorizationPolicy
        {
            ValueTask<bool> IAuthorizationPolicy.IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
                => ValueTask.FromResult(context.Principal.IsInRole("Clerk"));
        }

        public sealed class NegatedPolicy : IAuthorizationPolicy
        {
            public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
                => ValueTask.FromResult(!context.Principal.IsInRole("Banned"));
        }

        public static class PolicyValues
        {
            public static bool? Not(bool? value) => value.HasValue ? !value.Value : null;
            public static bool? And(bool? left, bool? right) => left == false || right == false ? false : left == true && right == true ? true : null;
        }

        public sealed class ThreeValuedPolicy : IAuthorizationPolicy
        {
            public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
                => ValueTask.FromResult(PolicyValues.Not(context.Principal.IsInRole("X")) == true);
        }

        public sealed class OpaquePolicy : IAuthorizationPolicy
        {
            public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
            {
                var unavailable = false;
                var allowed = context.Principal.IsInRole("X");
                return ValueTask.FromResult(allowed && !unavailable);
            }
        }

        public sealed class WeekdayPolicy : IAuthorizationPolicy
        {
            public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
                => ValueTask.FromResult(context.ReceivedAt.DayOfWeek == DayOfWeek.Monday);
        }

        public sealed class AsyncPolicy : IAuthorizationPolicy
        {
            public async ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
            {
                await Task.Yield();
                return context.Principal.IsInRole("X");
            }
        }

        public abstract class RolePolicy(string role) : IAuthorizationPolicy
        {
            public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
                => ValueTask.FromResult(context.Principal.IsInRole(role));
        }

        public sealed class AdminPolicy() : RolePolicy("Admin");
        """;

    /// <summary>
    /// Reduced, static, named-argument and Stage-shaped nested registrations.
    /// </summary>
    public const string Composition = """
        using Cratis.Arc.Authorization;
        using Microsoft.Extensions.DependencyInjection;

        namespace Library;

        public static class Composition
        {
            public static void Configure(IServiceCollection services)
            {
                services.AddArcAuthorizationPolicy<CanCompletePolicy>("CanComplete");
                services.AddArcAuthorizationPolicy<NestedPolicy>("Nested");
                AuthorizationPolicyServiceCollectionExtensions.AddArcAuthorizationPolicy<BlockPolicy>(services, "Block");
                services.AddArcAuthorizationPolicy<ConstructedPolicy>(name: "Constructed", evaluatesAnonymous: true);
                services.AddArcAuthorizationPolicy<ExplicitPolicy>("Explicit");
                services.AddArcAuthorizationPolicy<NegatedPolicy>("Negated");
                services.AddArcAuthorizationPolicy<ThreeValuedPolicy>("ThreeValued");
                services.AddArcAuthorizationPolicy<OpaquePolicy>("Opaque");
                services.AddArcAuthorizationPolicy<WeekdayPolicy>("Weekday");
                services.AddArcAuthorizationPolicy<AsyncPolicy>("Async");
                services.AddArcAuthorizationPolicy<AdminPolicy>("Field");
            }

            static void Register<T>(IServiceCollection services) where T : class, IAuthorizationPolicy
                => services.AddArcAuthorizationPolicy<T>("Generic");

            [System.Runtime.CompilerServices.ModuleInitializer]
            internal static void Init() => Registration.Add("AccountantOnly", services => services.AddArcAuthorizationPolicy<AccountantOnlyPolicy>("AccountantOnly"));
        }

        internal static class Registration
        {
            internal static void Add(string name, System.Action<IServiceCollection> apply) { }
        }
        """;

    /// <summary>
    /// A command referring to each registered policy.
    /// </summary>
    public const string Slice = """
        using Cratis.Arc.Authorization;
        using Cratis.Arc.Commands.ModelBound;

        namespace Library.Accounting.Approval;

        [Command, Authorize(Policy = "AccountantOnly")] public record AccountantOnlyCommand(string Id) { public void Handle() { } }
        [Command, Authorize(Policy = "CanComplete")] public record CanCompleteCommand(string Id) { public void Handle() { } }
        [Command, Authorize(Policy = "Nested")] public record NestedCommand(string Id) { public void Handle() { } }
        [Command, Authorize(Policy = "Block")] public record BlockCommand(string Id) { public void Handle() { } }
        [Command, Authorize(Policy = "Constructed")] public record ConstructedCommand(string Id) { public void Handle() { } }
        [Command, Authorize(Policy = "Explicit")] public record ExplicitCommand(string Id) { public void Handle() { } }
        [Command, Authorize(Policy = "Negated")] public record NegatedCommand(string Id) { public void Handle() { } }
        [Command, Authorize(Policy = "ThreeValued")] public record ThreeValuedCommand(string Id) { public void Handle() { } }
        [Command, Authorize(Policy = "Opaque")] public record OpaqueCommand(string Id) { public void Handle() { } }
        [Command, Authorize(Policy = "Weekday")] public record WeekdayCommand(string Id) { public void Handle() { } }
        [Command, Authorize(Policy = "Async")] public record AsyncCommand(string Id) { public void Handle() { } }
        [Command, Authorize(Policy = "Field")] public record FieldCommand(string Id) { public void Handle() { } }
        [Command, Authorize(Policy = "Generic")] public record GenericCommand(string Id) { public void Handle() { } }
        """;

    /// <summary>
    /// Gets every source file of the application.
    /// </summary>
    /// <returns>The source files and their paths.</returns>
    public static (string Path, string Text)[] All() =>
    [
        Analyzed.Root,
        ("Library/Policies.cs", Policies),
        ("Library/Composition.cs", Composition),
        ("Library/Accounting/Approval/Commands.cs", Slice)
    ];
}
