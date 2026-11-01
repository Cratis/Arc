// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SliceLocation } from '@cratis/arc/reflection';
import { SliceScope, SliceScopeKind } from '../../slices/SliceScope.js';
import { ApplicationContext } from '../ApplicationContext.js';

describe('when dialogs are opened and closed', () => {
    let context: ApplicationContext;
    let notifications: number;
    let afterFirst: string;
    let afterSecond: string;
    let afterSecondClosed: string;
    let afterBothClosed: string;
    let dialogIds: string[];

    beforeEach(() => {
        context = new ApplicationContext();
        notifications = 0;
        context.subscribe(() => notifications++);

        const page = new SliceScope(undefined, new SliceLocation('Orders', 'Editing', 'ChangeOrder'), SliceScopeKind.explicit);
        context.registerScope(page);

        const closeFirst = context.registerDialog({ id: 'one', title: 'Edit order', scope: new SliceScope(page) });
        afterFirst = context.getSnapshot().location.toString();
        const closeSecond = context.registerDialog({ id: 'two', scope: new SliceScope(page, new SliceLocation('Orders', 'Shipping', 'Address'), SliceScopeKind.explicit) });
        afterSecond = context.getSnapshot().location.toString();
        dialogIds = context.getSnapshot().dialogs.map(_ => _.id);
        closeSecond();
        afterSecondClosed = context.getSnapshot().location.toString();
        closeFirst();
        afterBothClosed = context.getSnapshot().location.toString();
    });

    it('should focus the dialog opened from the page', () => afterFirst.should.equal('Orders/Editing/ChangeOrder'));
    it('should focus the top-most dialog', () => afterSecond.should.equal('Orders/Shipping/Address'));
    it('should list the dialogs bottom first', () => dialogIds.should.deep.equal(['one', 'two']));
    it('should return focus to the dialog below when the top one closes', () => afterSecondClosed.should.equal('Orders/Editing/ChangeOrder'));
    it('should return focus to the page when all are closed', () => afterBothClosed.should.equal('Orders/Editing/ChangeOrder'));
    it('should notify subscribers', () => notifications.should.be.greaterThan(0));
});
