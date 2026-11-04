// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import { act, render } from '@testing-library/react';
import { ApplicationContext } from '../../applicationContext/ApplicationContext.js';
import { Host } from '../../applicationContext/for_useApplicationContext/Fixtures.js';
import { Slice } from '../../slices/Slice.js';
import { useDialog } from '../useDialog.js';

describe('when a background dialog title changes', () => {
    let titles: string[];
    let location: string;
    let activeSlices: string[];
    let activeSlicesBefore: string[];

    beforeEach(() => {
        const store = new ApplicationContext();
        let showA: () => void = () => { /* assigned during render */ };
        let showB: () => void = () => { /* assigned during render */ };
        let setTitle: (title: string) => void = () => { /* assigned during render */ };
        const Nothing = () => null;

        const Opener = () => {
            const [title, setTitleState] = useState('A1');
            setTitle = setTitleState;
            const [DialogA, showDialogA] = useDialog(Nothing, { title });
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
        activeSlicesBefore = store.getSnapshot().activeSlices.map(_ => _.toString());
        act(() => setTitle('A2'));
        titles = store.getSnapshot().dialogs.map(_ => _.title ?? '');
        location = store.getSnapshot().location.toString();
        activeSlices = store.getSnapshot().activeSlices.map(_ => _.toString());
        result.unmount();
    });

    it('should keep the dialogs in the order they were opened', () => titles.should.deep.equal(['A2', 'B']));
    it('should keep focus with the top-most dialog', () => location.should.equal('Orders/Shipping/Address'));
    it('should keep the active slices of the top-most dialog', () => activeSlices.should.deep.equal(activeSlicesBefore));
});
