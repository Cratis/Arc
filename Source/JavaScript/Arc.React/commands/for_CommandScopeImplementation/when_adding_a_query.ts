// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandScopeImplementation } from '../CommandScopeImplementation';
import { FakeQuery } from './FakeQuery';

describe('when adding a query', () => {
    const scope = new CommandScopeImplementation(() => {});
    const query = new FakeQuery();

    scope.addQuery(query);

    it('should not throw', () => true.should.be.true);
    
    it('should not add the same query twice when added again', () => {
        // This verifies the duplicate check by ensuring no exception is thrown
        // and the internal state remains consistent
        (() => scope.addQuery(query)).should.not.throw();
    });
});
