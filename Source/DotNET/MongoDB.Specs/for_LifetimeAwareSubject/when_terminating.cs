// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB.for_LifetimeAwareSubject;

public class when_terminating
{
    [Theory]
    [InlineData("completed")]
    [InlineData("failed")]
    [InlineData("disposed")]
    public void should_stop_once_and_preserve_the_terminal_notification(string termination)
    {
        var stops = 0;
        var completions = 0;
        var expectedError = new Exception("The producer failed.");
        Exception receivedError = null;
        using var subject = LifetimeAwareSubject<int>.Create(() => stops++);
        using var first = subject.Subscribe(_ => { }, error => receivedError = error, () => completions++);
        using var second = subject.Subscribe(_ => { }, _ => { });

        switch (termination)
        {
            case "completed":
                subject.OnCompleted();
                break;
            case "failed":
                subject.OnError(expectedError);
                break;
            default:
                subject.Dispose();
                break;
        }
        first.Dispose();
        second.Dispose();
        subject.Dispose();

        stops.ShouldEqual(1);
        completions.ShouldEqual(termination == "completed" ? 1 : 0);
        receivedError.ShouldEqual(termination == "failed" ? expectedError : null);
    }
}
