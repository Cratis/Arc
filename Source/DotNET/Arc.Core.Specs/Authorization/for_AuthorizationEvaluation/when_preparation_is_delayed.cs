// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation;

public class when_preparation_is_delayed : Specification
{
    readonly DateTimeOffset _received = new(2026, 9, 10, 11, 12, 13, TimeSpan.Zero);
    DateTimeOffset? _atPreparation;
    DateTimeOffset? _afterPreparation;
    DateTimeOffset? _replacementScopeReceipt;
    DateTimeOffset? _afterExit;

    async Task Because()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var released = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(_received);
        var services = new ServiceCollection().AddSingleton<TimeProvider>(clock).BuildServiceProvider();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "caller")], "test"));
        var principalAccessor = Substitute.For<ICurrentPrincipalAccessor>();
        principalAccessor.Current.Returns(principal);
        var anonymous = Substitute.For<IInstancesOf<IAnonymousEvaluator>>();
        anonymous.GetEnumerator().Returns(_ => new IAnonymousEvaluator[] { new AnonymousEvaluator() }.AsEnumerable().GetEnumerator());
        var attributes = Substitute.For<IInstancesOf<IAuthorizationAttributeEvaluator>>();
        attributes.GetEnumerator().Returns(_ => new IAuthorizationAttributeEvaluator[] { new AuthorizationAttributeEvaluator() }.AsEnumerable().GetEnumerator());
        var resolution = Substitute.For<IAuthorizationPolicyResolution>();
        resolution.SelectPrincipal(Arg.Any<ClaimsPrincipal?>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(call.ArgAt<ClaimsPrincipal?>(0)));
        var runtime = Substitute.For<IAuthorizationPolicyRuntime>();
        runtime.Resolve(Arg.Any<IReadOnlyList<AuthorizationRequirement>>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                _atPreparation = new OperationContextAccessor().ReceivedAt;
                entered.TrySetResult();
                await released.Task;
                return resolution;
            });
        var evaluation = new AuthorizationEvaluation(new AuthorizationDeclarations(anonymous, attributes), Substitute.For<IAuthorizationEvaluator>(), principalAccessor, runtime);
        using (OperationContextScope.Begin(services))
        {
            // Dispatch received the operation before authentication preparation began.
            clock.GetUtcNow().Returns(_received.AddDays(1));
            var preparation = evaluation.Prepare(typeof(ProtectedCommand), services, CancellationToken.None);
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            released.TrySetResult();
            await preparation;
            _afterPreparation = new OperationContextAccessor().ReceivedAt;
            await using var replacement = new ServiceCollection()
                .AddSingleton<IOperationContextAccessor, OperationContextAccessor>()
                .BuildServiceProvider();
            await using var replacementScope = replacement.CreateAsyncScope();
            _replacementScopeReceipt = replacementScope.ServiceProvider.GetRequiredService<IOperationContextAccessor>().ReceivedAt;
        }
        _afterExit = new OperationContextAccessor().ReceivedAt;
    }

    [Fact] void should_keep_the_dispatch_receipt_when_preparation_starts_later() => _atPreparation.ShouldEqual(_received);
    [Fact] void should_keep_the_original_receipt_after_preparation() => _afterPreparation.ShouldEqual(_received);
    [Fact] void should_expose_the_same_receipt_in_a_replacement_scope() => _replacementScopeReceipt.ShouldEqual(_received);
    [Fact] void should_restore_the_previous_operation_after_preparation() => _afterExit.ShouldBeNull();

    [Authorize]
    public record ProtectedCommand;
}
