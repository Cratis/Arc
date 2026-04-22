// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Arc.MongoDB.for_LifetimeAwareSubject;

public class when_disposing_last_subscription
{
    [Fact]
    public void should_invoke_the_cleanup_callback()
    {
        var callbackInvocations = 0;
        var subject = new LifetimeAwareSubject<int>(new Subject<int>(), () => callbackInvocations++);

        var firstSubscription = subject.Subscribe(_ => { });
        var secondSubscription = subject.Subscribe(_ => { });

        firstSubscription.Dispose();
        callbackInvocations.ShouldEqual(0);

        secondSubscription.Dispose();
        callbackInvocations.ShouldEqual(1);
    }
}