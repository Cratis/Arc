// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.PersonalAccessTokens.Contract;
using Cratis.Arc.PersonalAccessTokens.Development;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.PersonalAccessTokens.for_DevelopmentPatAdapters.given;

public class a_development_pat_installation : Specification
{
    protected IHostEnvironment _environment;
    protected DevelopmentPatAuthority _authority;
    protected DevelopmentPatRecoveryEpoch _epochs;
    protected DevelopmentPatPolicy _policy;
    private protected PatContractFixture _facts;
    protected DevelopmentClock _clock;

    void Establish()
    {
        _environment = Substitute.For<IHostEnvironment>();
        _environment.EnvironmentName.Returns(Environments.Development);
        _clock = new();
        _authority = new(_environment, _clock);
        _epochs = new(_environment);
        _facts = new(new(_authority, _epochs, Substitute.For<IPatPolicy>()), 1);
        _policy = new(_environment, _facts.Settings, _clock);
        _facts = new(new(_authority, _epochs, _policy), 1);
    }

    public class DevelopmentClock : TimeProvider
    {
        public TimeSpan Offset { get; set; }

        public override DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow + Offset;
    }
}
