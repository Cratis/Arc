// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import { act, render } from '@testing-library/react';
import { SliceLocation } from '@cratis/arc/reflection';
import { ApplicationContext } from '../ApplicationContext.js';
import { ApplicationContextContributor } from '../ApplicationContextContributor.js';
import { Slice } from '../../slices/Slice.js';
import { Host, TopBar, UsesSlice } from './Fixtures.js';

describe('when read outside every slice scope', () => {
    let initial: string;
    let withBoth: string[];
    let screen: string;
    let version: unknown;
    let afterUnmount: string[];

    beforeEach(() => {
        let setShown: (value: boolean) => void = () => { /* assigned during render */ };
        let latest: ReturnType<typeof import('../useApplicationContext.js').useApplicationContext> | undefined;
        const Page = () => {
            const [shown, set] = useState(false);
            setShown = set;
            return (
                <Slice>
                    <UsesSlice location={new SliceLocation('Billing', 'Invoices', 'Pay')} />
                    {shown && <UsesSlice location={new SliceLocation('Billing', 'Invoices', 'Listing')} />}
                </Slice>
            );
        };

        const result = render(
            <Host store={new ApplicationContext()}>
                <ApplicationContextContributor screen={{ id: 'invoices', name: 'Invoices', path: '/invoices' }} values={{ version: '3.1' }}>
                    <TopBar onRender={snapshot => latest = snapshot} />
                    <Page />
                </ApplicationContextContributor>
            </Host>);

        initial = result.getByTestId('top-bar').textContent!;
        act(() => setShown(true));
        withBoth = latest!.activeSlices.map(_ => _.toString());
        screen = latest!.screen.name;
        version = latest!.contributions.version;
        act(() => setShown(false));
        afterUnmount = latest!.activeSlices.map(_ => _.toString());
        result.unmount();
    });

    it('should read the location of the current page', () => initial.should.equal('Billing/Invoices/Pay'));
    it('should list every active slice', () => withBoth.should.have.members(['Billing/Invoices/Pay', 'Billing/Invoices/Listing']));
    it('should update when a slice unmounts', () => afterUnmount.should.deep.equal(['Billing/Invoices/Pay']));
    it('should include the contributed screen', () => screen.should.equal('Invoices'));
    it('should include contributed values', () => (version as string).should.equal('3.1'));
});
