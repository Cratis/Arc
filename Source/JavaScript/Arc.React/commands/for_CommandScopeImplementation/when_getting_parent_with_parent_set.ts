// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandScopeImplementation } from '../CommandScopeImplementation';

describe('when getting parent with parent set', () => {
    const parentScope = new CommandScopeImplementation(() => {});
    const childScope = new CommandScopeImplementation(() => {}, undefined, undefined, parentScope);

    it('should return the parent', () => childScope.parent.should.equal(parentScope));
});
