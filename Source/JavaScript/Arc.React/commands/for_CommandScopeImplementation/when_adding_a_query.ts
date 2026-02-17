// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandScopeImplementation } from '../CommandScopeImplementation';
import { FakeQuery } from './FakeQuery';

describe('when adding a query', () => {
    const scope = new CommandScopeImplementation(() => {});
    const query = new FakeQuery();

    scope.addQuery(query);

    it('should not throw', () => true.should.be.true);
});
