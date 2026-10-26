// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SliceLocation } from '@cratis/arc/reflection';
import { SliceScope } from '../SliceScope.js';

describe('when a registration is removed', () => {
    let scope: SliceScope;
    let notifications: number;

    beforeEach(() => {
        scope = new SliceScope();
        notifications = 0;
        const unregister = scope.register(new SliceLocation('Billing', 'Invoices', 'Pay'));
        scope.subscribe(() => notifications++);
        unregister();
        unregister();
    });

    it('should resolve to the empty location', () => scope.resolve().should.equal(SliceLocation.empty));
    it('should notify once', () => notifications.should.equal(1));
});
