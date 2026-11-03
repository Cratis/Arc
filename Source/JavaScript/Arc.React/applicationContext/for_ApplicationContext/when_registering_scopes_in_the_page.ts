// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SliceLocation } from '@cratis/arc/reflection';
import { SliceScope } from '../../slices/SliceScope.js';
import { ApplicationContext } from '../ApplicationContext.js';

describe('when registering scopes in the page', () => {
    let context: ApplicationContext;
    let before: unknown;
    let removeSecond: () => void;

    beforeEach(() => {
        context = new ApplicationContext();
        const root = new SliceScope();
        const first = new SliceScope(root);
        const second = new SliceScope(root);
        first.register(new SliceLocation('Billing', 'Invoices', 'Pay'));
        first.register(new SliceLocation('Billing', 'Invoices', 'Pay'));
        second.register(new SliceLocation('Billing', 'Invoices', 'Listing'));
        context.registerScope(root);
        context.registerScope(first);
        removeSecond = context.registerScope(second);
        before = context.getSnapshot();
    });

    it('should fold the location of the leaf scopes', () => context.getSnapshot().location.toString().should.equal('Billing/Invoices'));
    it('should list every distinct slice before folding', () => context.getSnapshot().activeSlices.map(_ => _.toString()).should.have.members(['Billing/Invoices/Pay', 'Billing/Invoices/Listing']));
    it('should keep the same snapshot while nothing changes', () => context.getSnapshot().should.equal(before));
    it('should have no dialogs', () => context.getSnapshot().dialogs.length.should.equal(0));
    it('should have an empty screen', () => context.getSnapshot().screen.id.should.equal(''));

    describe('and one of them is removed', () => {
        beforeEach(() => removeSecond());

        it('should only list the remaining slice', () => context.getSnapshot().activeSlices.map(_ => _.toString()).should.deep.equal(['Billing/Invoices/Pay']));
        it('should resolve the location of the remaining slice', () => context.getSnapshot().location.toString().should.equal('Billing/Invoices/Pay'));
    });
});
