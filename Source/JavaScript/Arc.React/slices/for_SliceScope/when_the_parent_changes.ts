// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SliceLocation } from '@cratis/arc/reflection';
import { SliceScope } from '../SliceScope.js';

describe('when the parent changes', () => {
    let notificationsWhileSubscribed: number;
    let notificationsAfterUnsubscribing: number;

    beforeEach(() => {
        const parent = new SliceScope();
        const child = new SliceScope(parent);
        let notifications = 0;
        const unsubscribe = child.subscribe(() => notifications++);
        parent.register(new SliceLocation('Billing', 'Invoices', 'Pay'));
        notificationsWhileSubscribed = notifications;
        unsubscribe();
        parent.register(new SliceLocation('Billing', 'Invoices', 'Listing'));
        notificationsAfterUnsubscribing = notifications - notificationsWhileSubscribed;
    });

    it('should notify subscribers of the child', () => notificationsWhileSubscribed.should.equal(1));
    it('should stop notifying once the child has no subscribers', () => notificationsAfterUnsubscribing.should.equal(0));
});
