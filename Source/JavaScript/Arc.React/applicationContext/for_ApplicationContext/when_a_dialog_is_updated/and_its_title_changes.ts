// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SliceLocation } from '@cratis/arc/reflection';
import { SliceScope, SliceScopeKind } from '../../../slices/SliceScope.js';
import { ApplicationContext } from '../../ApplicationContext.js';
import { ApplicationDialogRegistration } from '../../ApplicationDialogRegistration.js';

describe('when a dialog is updated and its title changes', () => {
    let context: ApplicationContext;
    let page: SliceScope;
    let one: ApplicationDialogRegistration;
    let oneScope: SliceScope;
    let notifications: number;

    beforeEach(() => {
        context = new ApplicationContext();
        page = new SliceScope(undefined, new SliceLocation('Orders', 'Editing', 'ChangeOrder'), SliceScopeKind.explicit);
        context.registerScope(page);
        oneScope = new SliceScope(page);
        one = context.trackDialog({ id: 'one', title: 'A1', scope: oneScope });
        context.trackDialog({ id: 'two', scope: new SliceScope(page, new SliceLocation('Orders', 'Shipping', 'Address'), SliceScopeKind.explicit) });
        context.getSnapshot();
        notifications = 0;
        context.subscribe(() => notifications++);
        one.update({ title: 'A2', scope: oneScope });
    });

    it('should keep the order of the dialogs', () => context.getSnapshot().dialogs.map(_ => _.id).should.deep.equal(['one', 'two']));
    it('should change the title in place', () => context.getSnapshot().dialogs[0].title!.should.equal('A2'));
    it('should keep focus with the top-most dialog', () => context.getSnapshot().location.toString().should.equal('Orders/Shipping/Address'));
    it('should notify subscribers', () => notifications.should.be.greaterThan(0));
});
