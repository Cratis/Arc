// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SliceLocation } from '@cratis/arc/reflection';
import { SliceScope, SliceScopeKind } from '../../slices/SliceScope.js';
import { ApplicationContext } from '../ApplicationContext.js';

class LegacyContext extends ApplicationContext {
    override registerDialog(dialog: { id: string, title?: string, scope: SliceScope }): () => void {
        return super.registerDialog(dialog);
    }
}

describe('when removing a dialog through its cleanup', () => {
    let context: LegacyContext;
    let notifications: number;
    let before: string[];
    let after: string[];

    beforeEach(() => {
        context = new LegacyContext();
        const page = new SliceScope(undefined, new SliceLocation('Orders', 'Editing', 'ChangeOrder'), SliceScopeKind.explicit);
        context.registerScope(page);
        const scope = new SliceScope(page, new SliceLocation('Orders', 'Shipping', 'Address'), SliceScopeKind.explicit);
        const remove = context.registerDialog({ id: 'one', scope });
        before = context.getSnapshot().dialogs.map(_ => _.id);
        notifications = 0;
        context.subscribe(() => notifications++);
        remove();
        remove();
        after = context.getSnapshot().dialogs.map(_ => _.id);
        notifications = 0;
        scope.register(new SliceLocation('Orders', 'Shipping', 'Other'));
    });

    it('should have registered the dialog', () => before.should.deep.equal(['one']));
    it('should remove the dialog', () => after.should.deep.equal([]));
    it('should stop listening to the scope of the dialog', () => notifications.should.equal(0));
});
