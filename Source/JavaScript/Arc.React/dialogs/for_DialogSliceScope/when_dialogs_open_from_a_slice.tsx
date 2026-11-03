// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, render } from '@testing-library/react';
import { ApplicationContext } from '../../applicationContext/ApplicationContext.js';
import { Host } from '../../applicationContext/for_useApplicationContext/Fixtures.js';
import { Slice } from '../../slices/Slice.js';
import { useSliceContext } from '../../slices/useSliceContext.js';
import { useDialog } from '../useDialog.js';
import { useDialogContext } from '../DialogContext.js';
import { DialogResult } from '../DialogResult.js';
import { useDialogSliceLocation } from '../useDialogSliceLocation.js';

describe('when dialogs open from a slice', () => {
    let store: ApplicationContext;
    let insideDialog: string;
    let dialogLocation: string;
    let explicitLocation: string;
    let outsideDialog: string;
    let focusedWhileOpen: string;
    let focusedWithExplicit: string;
    let focusedAfterClose: string;
    let openedFocus: string[];
    let dialogCount: number;

    beforeEach(() => {
        store = new ApplicationContext();
        let showPlain: () => void = () => { /* assigned during render */ };
        let showExplicit: () => void = () => { /* assigned during render */ };
        let showRefining: () => void = () => { /* assigned during render */ };
        let closePlain: () => void = () => { /* assigned during render */ };

        const Probe = (props: { onRender: (sliceContext: string, dialog: string) => void }) => {
            props.onRender(useSliceContext().toString(), useDialogSliceLocation().toString());
            return null;
        };

        const Plain = () => {
            const { closeDialog } = useDialogContext();
            closePlain = () => closeDialog(DialogResult.Ok);
            return <Probe onRender={(slice, dialog) => { insideDialog = slice; dialogLocation = dialog; }} />;
        };
        const Nothing = () => null;
        const Explicit = () => <Probe onRender={(_, dialog) => explicitLocation = dialog} />;

        // The dialogs are rendered by a renderer at the root, outside the slice that opens them.
        const Opener = () => {
            const [PlainDialog, showPlainDialog] = useDialog(Plain);
            const [ExplicitDialog, showExplicitDialog] = useDialog(Explicit, { slice: { module: 'Orders', feature: 'Shipping', slice: 'Address' }, title: 'Address' });
            const [RefiningDialog, showRefiningDialog] = useDialog(Nothing);
            showPlain = () => { void showPlainDialog(); };
            showExplicit = () => { void showExplicitDialog(); };
            showRefining = () => { void showRefiningDialog(); };
            return <><PlainDialog /><ExplicitDialog /><RefiningDialog /></>;
        };

        const result = render(
            <Host store={store}>
                <Probe onRender={slice => outsideDialog = slice} />
                <Slice module="Orders" feature="Editing" slice="ChangeOrder"><Opener /></Slice>
            </Host>);

        act(() => showPlain());
        focusedWhileOpen = store.getSnapshot().location.toString();
        act(() => showExplicit());
        focusedWithExplicit = store.getSnapshot().location.toString();
        openedFocus = store.getSnapshot().dialogs.map(_ => _.title ?? '');
        act(() => showRefining());
        dialogCount = store.getSnapshot().dialogs.length;
        act(() => closePlain());
        focusedAfterClose = store.getSnapshot().location.toString();
        result.unmount();
    });

    it('should report the opener location from inside the dialog', () => insideDialog.should.equal('Orders/Editing/ChangeOrder'));
    it('should report the opener location through useDialogSliceLocation', () => dialogLocation.should.equal('Orders/Editing/ChangeOrder'));
    it('should report empty outside any dialog', () => outsideDialog.should.equal(''));
    it('should focus the open dialog', () => focusedWhileOpen.should.equal('Orders/Editing/ChangeOrder'));
    it('should let an explicit location win', () => explicitLocation.should.equal('Orders/Shipping/Address'));
    it('should focus the explicit location of the top-most dialog', () => focusedWithExplicit.should.equal('Orders/Shipping/Address'));
    it('should list open dialogs with their titles', () => openedFocus.should.deep.equal(['', 'Address']));
    it('should list all open dialogs', () => dialogCount.should.equal(3));
    it('should keep a closed dialog out of the stack', () => focusedAfterClose.should.not.equal(''));
});
