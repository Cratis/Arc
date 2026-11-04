// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { StrictMode, useState } from 'react';
import { act, render } from '@testing-library/react';
import { ApplicationContext } from '../../applicationContext/ApplicationContext.js';
import { Host } from '../../applicationContext/for_useApplicationContext/Fixtures.js';
import { Slice } from '../../slices/Slice.js';
import { useDialog } from '../useDialog.js';

describe('when dialogs open in strict mode', () => {
    let titles: string[];
    let location: string;
    let dialogCount: number;

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
            <StrictMode><Host store={store}>
                <Slice module="Orders" feature="Editing" slice="ChangeOrder"><Opener /></Slice>
            </Host></StrictMode>);

        act(() => showA());
        act(() => showB());
        act(() => setTitle('A2'));
        titles = store.getSnapshot().dialogs.map(_ => _.title ?? '');
        location = store.getSnapshot().location.toString();
        dialogCount = store.getSnapshot().dialogs.length;
        result.unmount();
    });

    it('should keep the dialogs in the order they were opened', () => titles.should.deep.equal(['A2', 'B']));
    it('should keep focus with the top-most dialog', () => location.should.equal('Orders/Shipping/Address'));
    it('should not duplicate dialogs', () => dialogCount.should.equal(2));
});
