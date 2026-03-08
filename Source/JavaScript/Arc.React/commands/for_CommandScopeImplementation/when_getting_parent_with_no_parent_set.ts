// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandScopeImplementation } from '../CommandScopeImplementation';

describe('when getting parent with no parent set', () => {
    const scope = new CommandScopeImplementation(() => {});

    it('should return undefined', () => (scope.parent === undefined).should.be.true);
});
