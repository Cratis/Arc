// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Commands.for_ReadModelKeyProviders;

public class when_the_provider_throws : Specification
{
    Exception _exception;
    ILogger _logger;

    void Because()
    {
        _logger = Substitute.For<ILogger>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        using var frame = AuthorizationCommandIdentity.Enter(new ClaimsPrincipal(new ClaimsIdentity([], "test")));
        _exception = Catch.Exception(() => ReadModelKeyProviders.Resolve(new LookUpByAttribute<ThrowingKey>(), typeof(object), typeof(object), null, _logger));
    }

    [Fact] void should_deny_resolution() => ((ReadModelKeyResolutionDenied)_exception).Reason.ShouldEqual(ReadModelKeyRefusal.ProviderFailed);
    [Fact] void should_discard_the_provider_exception() => _exception.InnerException.ShouldBeNull();
    [Fact] void should_not_expose_the_provider_message() => _exception.ToString().Contains("private-claim", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_log_the_provider_message() => _logger.ReceivedCalls().SelectMany(call => call.GetArguments()).Any(argument => argument?.ToString()?.Contains("private-claim", StringComparison.Ordinal) == true).ShouldBeFalse();

    public sealed class ThrowingKey : IReadModelKeyProvider
    {
        public ReadModelKeyLookup Resolve(ReadModelKeyContext context) => throw new InvalidOperationException("private-claim");
    }
}
