// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB.for_LifetimeAwareSubject;

public class when_subscribing_after_termination
{
    [Theory]
    [InlineData("completed")]
    [InlineData("failed")]
    public void should_deliver_the_terminal_notification_to_the_late_subscriber(string termination)
    {
        var stops = 0;
        var completions = 0;
        var expectedError = new Exception("The producer failed.");
        Exception receivedError = null;
        var subject = LifetimeAwareSubject<int>.Create(() => stops++);

        if (termination == "failed")
        {
            subject.OnError(expectedError);
        }
        else
        {
            subject.OnCompleted();
        }
        subject.Dispose();

        using var late = subject.Subscribe(_ => { }, error => receivedError = error, () => completions++);
        late.Dispose();

        stops.ShouldEqual(1);
        completions.ShouldEqual(termination == "completed" ? 1 : 0);
        receivedError.ShouldEqual(termination == "failed" ? expectedError : null);
    }
}
