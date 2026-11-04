// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import { act, render } from '@testing-library/react';
import { SliceLocation } from '@cratis/arc/reflection';
import { ApplicationContext } from '../../applicationContext/ApplicationContext.js';
import { Host } from '../../applicationContext/for_useApplicationContext/Fixtures.js';
import { SliceScope, SliceScopeKind } from '../../slices/SliceScope.js';
import { DialogSliceScope } from '../DialogSliceScope.js';

describe('when tracking of a dialog is toggled', () => {
    let ids: string[];
    let location: string;

    beforeEach(() => {
        const store = new ApplicationContext();
        let setTrack: (track: boolean) => void = () => { /* assigned during render */ };
        const opener = new SliceScope(undefined, new SliceLocation('Orders', 'Editing', 'ChangeOrder'), SliceScopeKind.explicit);

        const Dialogs = () => {
            const [track, setTrackState] = useState(true);
            setTrack = setTrackState;
            return (
                <>
                    <DialogSliceScope id="a" opener={opener} title="A" track={track} />
                    <DialogSliceScope id="b" opener={opener} title="B" slice={{ module: 'Orders', feature: 'Shipping', slice: 'Address' }} track />
                </>);
        };

        const result = render(<Host store={store}><Dialogs /></Host>);
        act(() => setTrack(false));
        act(() => setTrack(true));
        ids = store.getSnapshot().dialogs.map(_ => _.id);
        location = store.getSnapshot().location.toString();
        result.unmount();
    });

    it('should register the dialog again on top', () => ids.should.deep.equal(['b', 'a']));
    it('should focus the dialog registered again', () => location.should.equal('Orders/Editing/ChangeOrder'));
});
