// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SliceLocation } from '@cratis/arc/reflection';
import { SliceScope } from '../SliceScope.js';

describe('when resolving with one registration', () => {
    const location = new SliceLocation('Billing', 'Invoices', 'Pay');
    let scope: SliceScope;

    beforeEach(() => {
        const parent = new SliceScope();
        parent.register(new SliceLocation('Sales', 'Orders', 'Place'));
        scope = new SliceScope(parent);
        scope.register(location);
    });

    it('should resolve to the registered location rather than the parent', () => scope.resolve().equals(location).should.be.true);
});
