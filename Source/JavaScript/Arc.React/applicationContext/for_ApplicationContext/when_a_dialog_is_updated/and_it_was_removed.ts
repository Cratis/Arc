// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SliceLocation } from '@cratis/arc/reflection';
import { SliceScope, SliceScopeKind } from '../../../slices/SliceScope.js';
import { ApplicationContext } from '../../ApplicationContext.js';
import { ApplicationDialogRegistration } from '../../ApplicationDialogRegistration.js';

describe('when a dialog is updated and it was removed', () => {
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
        one.remove();
        context.getSnapshot();
        notifications = 0;
        one.update({ title: 'A2', scope: new SliceScope(page, new SliceLocation('Orders', 'Editing', 'Review'), SliceScopeKind.explicit) });
        one.remove();
    });

    it('should not add the dialog again', () => context.getSnapshot().dialogs.map(_ => _.id).should.deep.equal(['two']));
    it('should not notify subscribers', () => notifications.should.equal(0));
    it('should stop listening to the scope of the removed dialog', () => {
        oneScope.register(new SliceLocation('Orders', 'Editing', 'Other'));
        notifications.should.equal(0);
    });
});
