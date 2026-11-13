// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace OAuthPatCallerAcceptance.Specs.given;

public abstract class a_refused_call : a_composed_host
{
    protected virtual int ExpectedOAuthReads => 0;
    protected virtual int ExpectedPatReads => 0;
    protected virtual HttpStatusCode? ExpectedStatus => HttpStatusCode.Unauthorized;

    protected void AssertBothAttempted() => _outcomes.Count.ShouldEqual(2);
    protected void AssertBothRefused() => _outcomes.TrueForAll(outcome => !outcome.Accepted && outcome.Status == ExpectedStatus && outcome.SafeRefusal).ShouldBeTrue();
    protected void AssertNoDispatch() => _store.Dispatches.ShouldEqual(0);
    protected void AssertOAuthReads() => _store.OAuthReads.ShouldEqual(ExpectedOAuthReads);
    protected void AssertPatReads() => _store.PatReads.ShouldEqual(ExpectedPatReads);
}
