// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryScopeImplementation } from '../QueryScopeImplementation';

describe('when child scope is performing', () => {
    const parentScope = new QueryScopeImplementation();
    const childScope = new QueryScopeImplementation(undefined, parentScope);

    childScope.notifyPerformingStarted();

    it('should report parent is performing because child is performing', () => parentScope.isPerforming.should.be.true);
    it('should report child is performing', () => childScope.isPerforming.should.be.true);
});
