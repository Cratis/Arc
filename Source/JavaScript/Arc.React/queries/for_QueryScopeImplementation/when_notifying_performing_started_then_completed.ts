// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryScopeImplementation } from '../QueryScopeImplementation';

describe('when notifying performing started then completed', () => {
    let lastIsPerformingCallback = false;

    const scope = new QueryScopeImplementation((value) => {
        lastIsPerformingCallback = value;
    });

    scope.notifyPerformingStarted();
    scope.notifyPerformingCompleted();

    it('should not be performing', () => scope.isPerforming.should.be.false);
    it('should call the callback with false', () => lastIsPerformingCallback.should.be.false);
});
