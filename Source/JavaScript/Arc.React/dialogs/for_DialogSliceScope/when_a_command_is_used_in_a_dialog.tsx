// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, render } from '@testing-library/react';
import { SliceLocation } from '@cratis/arc/reflection';
import { ApplicationContext } from '../../applicationContext/ApplicationContext.js';
import { Host } from '../../applicationContext/for_useApplicationContext/Fixtures.js';
import { Slice } from '../../slices/Slice.js';
import { useRegisterSliceLocation } from '../../slices/useRegisterSliceLocation.js';
import { useSliceContext } from '../../slices/useSliceContext.js';
import { useDialog } from '../useDialog.js';

describe('when a command is used in a dialog', () => {
    let insideDialog: string;
    let focused: string;

    beforeEach(() => {
        const store = new ApplicationContext();
        let show: () => void = () => { /* assigned during render */ };

        const UsesCommand = () => {
            useRegisterSliceLocation(new SliceLocation('Orders', 'Editing', 'Audit'));
            insideDialog = useSliceContext().toString();
            return null;
        };

        const Opener = () => {
            const [Dialog, showDialog] = useDialog(UsesCommand);
            show = () => { void showDialog(); };
            return <Dialog />;
        };

        const result = render(
            <Host store={store}>
                <Slice><Opener /></Slice>
            </Host>);

        act(() => show());
        focused = store.getSnapshot().location.toString();
        result.unmount();
    });

    it('should refine the location of the dialog as it would inline', () => insideDialog.should.equal('Orders/Editing/Audit'));
    it('should focus the refined location', () => focused.should.equal('Orders/Editing/Audit'));
});
