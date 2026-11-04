// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import { act, render } from '@testing-library/react';
import { ApplicationContext } from '../../applicationContext/ApplicationContext.js';
import { Host } from '../../applicationContext/for_useApplicationContext/Fixtures.js';
import { Slice } from '../../slices/Slice.js';
import { useDialog } from '../useDialog.js';

describe('when a background dialog slice changes', () => {
    let ids: string[];
    let backgroundLocation: string;
    let location: string;

    beforeEach(() => {
        const store = new ApplicationContext();
        let showA: () => void = () => { /* assigned during render */ };
        let showB: () => void = () => { /* assigned during render */ };
        let setSlice: (slice: string) => void = () => { /* assigned during render */ };
        const Nothing = () => null;

        const Opener = () => {
            const [slice, setSliceState] = useState('ChangeOrder');
            setSlice = setSliceState;
            const [DialogA, showDialogA] = useDialog(Nothing, { title: 'A', slice: { module: 'Orders', feature: 'Editing', slice } });
            const [DialogB, showDialogB] = useDialog(Nothing, { title: 'B', slice: { module: 'Orders', feature: 'Shipping', slice: 'Address' } });
            showA = () => { void showDialogA(); };
            showB = () => { void showDialogB(); };
            return <><DialogA /><DialogB /></>;
        };

        const result = render(
            <Host store={store}>
                <Slice module="Orders" feature="Editing" slice="ChangeOrder"><Opener /></Slice>
            </Host>);

        act(() => showA());
        act(() => showB());
        act(() => setSlice('Review'));
        ids = store.getSnapshot().dialogs.map(_ => _.title ?? '');
        backgroundLocation = store.getSnapshot().dialogs[0].location.toString();
        location = store.getSnapshot().location.toString();
        result.unmount();
    });

    it('should keep the dialogs in the order they were opened', () => ids.should.deep.equal(['A', 'B']));
    it('should update the location of the background dialog', () => backgroundLocation.should.equal('Orders/Editing/Review'));
    it('should keep focus with the top-most dialog', () => location.should.equal('Orders/Shipping/Address'));
});
