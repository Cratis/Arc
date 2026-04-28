// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryScopeImplementation } from '../QueryScopeImplementation';

describe('when notifying performing started twice then completed once', () => {
    let callbackCallCount = 0;
    let lastIsPerformingCallback = false;

    const scope = new QueryScopeImplementation((value) => {
        callbackCallCount++;
        lastIsPerformingCallback = value;
    });

    scope.notifyPerformingStarted();
    scope.notifyPerformingStarted();
    scope.notifyPerformingCompleted();

    it('should still be performing', () => scope.isPerforming.should.be.true);
    it('should only call the callback once', () => callbackCallCount.should.equal(1));
    it('should have called the callback with true', () => lastIsPerformingCallback.should.be.true);
});
