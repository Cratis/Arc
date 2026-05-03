// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryScopeImplementation } from '../QueryScopeImplementation';

describe('when notifying performing started', () => {
    let isPerformingCallback = false;

    const scope = new QueryScopeImplementation((value) => {
        isPerformingCallback = value;
    });

    scope.notifyPerformingStarted();

    it('should be performing', () => scope.isPerforming.should.be.true);
    it('should call the callback with true', () => isPerformingCallback.should.be.true);
});
