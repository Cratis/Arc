// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

internal static class StageShapedPolicySources
{
    internal const string Policies = """
        namespace Library;

        public sealed class AccountantOnlyPolicy : global::Cratis.Arc.Authorization.IAuthorizationPolicy
        {
            public global::System.Threading.Tasks.ValueTask<bool> IsAuthorized(global::Cratis.Arc.Authorization.AuthorizationPolicyContext context, global::System.Threading.CancellationToken cancellationToken)
                => global::System.Threading.Tasks.ValueTask.FromResult((context.Principal.Identity?.IsAuthenticated == true && context.Principal.IsInRole("Accountant")));
        }

        public sealed class CanCompletePolicy : global::Cratis.Arc.Authorization.IAuthorizationPolicy
        {
            public global::System.Threading.Tasks.ValueTask<bool> IsAuthorized(global::Cratis.Arc.Authorization.AuthorizationPolicyContext context, global::System.Threading.CancellationToken cancellationToken)
                => global::System.Threading.Tasks.ValueTask.FromResult(context.Principal.IsInRole("Automation"));
        }
        """;

    internal const string Composition = """
        using Cratis.Arc.Authorization;
        using Microsoft.Extensions.DependencyInjection;

        namespace Library;

        internal static class Registration
        {
            internal static void Add(string name, System.Action<IServiceCollection> apply) { }
        }

        internal static class Policies
        {
            [System.Runtime.CompilerServices.ModuleInitializer]
            internal static void Init()
            {
                Registration.Add("AccountantOnly", services => services.AddArcAuthorizationPolicy<AccountantOnlyPolicy>("AccountantOnly"));
                Registration.Add("CanComplete", services => services.AddArcAuthorizationPolicy<CanCompletePolicy>("CanComplete"));
            }
        }
        """;

    internal const string Commands = """
        using Cratis.Arc.Authorization;
        using Cratis.Arc.Commands.ModelBound;

        namespace Library.Accounting.Approval;

        [Command, Authorize(Policy = "AccountantOnly")]
        public record ApproveInvoice(string Id) { public void Handle() { } }

        [Command, Authorize(Policy = "CanComplete")]
        public record CompleteInvoice(string Id) { public void Handle() { } }
        """;

    internal static (string Path, string Text)[] All() =>
    [
        Analyzed.Root,
        ("Library/Policies.cs", Policies),
        ("Library/Composition.cs", Composition),
        ("Library/Accounting/Approval/Commands.cs", Commands)
    ];
}
