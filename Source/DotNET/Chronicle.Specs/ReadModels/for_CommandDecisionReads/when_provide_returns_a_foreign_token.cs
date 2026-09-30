// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads;

public class when_provide_returns_a_foreign_token : Specification
{
    Exception _exception;

    void Because()
    {
        CommandDecisionReads.Begin(typeof(object));
        try
        {
            _exception = Record.Exception(() => CommandDecisionReads.VerifyProvided(
                DecisionRead<object>.Unprotected((ReadModelKey)"other", new object())));
        }
        finally
        {
            CommandDecisionReads.End();
        }
    }

    [Fact] void should_refuse_a_token_not_issued_for_this_invocation() => _exception.ShouldBeOfExactType<DecisionReadNotIssuedForInvocation>();
}
