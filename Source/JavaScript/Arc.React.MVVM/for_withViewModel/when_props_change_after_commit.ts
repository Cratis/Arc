// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// @vitest-environment jsdom

import { createElement, StrictMode, Suspense } from 'react';
import { render, renderHook, cleanup, act } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { ArcContext } from '@cratis/arc.react';
import { withViewModel } from '../withViewModel.js';
import { usePropsChanged } from '../usePropsChanged.js';
import type { IPropsChanged } from '../IPropsChanged.js';
import { flushDisposal } from './given/a_view_model_with_cleanup.js';

type Props = { value: { label: string } };
const initial: Props = { value: { label: 'initial' } };
const changed: Props = { value: { label: 'changed' } };
const calls: { next: Props; previous: Props; text: string | null | undefined }[] = [];
const legacy: Props[] = [];
let constructed = 0;
class ViewModel implements IPropsChanged<Props> {
    constructor() { constructed++; }
    handleProps(props: Props) { legacy.push(props); }
    propsChanged(next: Props, previous: Props) {
        calls.push({ next, previous, text: document.querySelector('[data-testid=value]')?.textContent });
    }
}
const View = withViewModel<ViewModel, Props>(ViewModel, ({ props }) => createElement('span', { 'data-testid': 'value' }, props.value.label));
const tree = (props: Props, strict: boolean, development: boolean) => {
    const view = createElement(MemoryRouter, null, createElement(ArcContext.Provider,
        { value: { microservice: '', development } }, createElement(View, props)));
    return strict ? createElement(StrictMode, null, view) : view;
};

describe('when props change after commit', () => {
    beforeEach(() => { calls.length = 0; legacy.length = 0; constructed = 0; });
    afterEach(async () => { cleanup(); await flushDisposal(); });

    for (const strict of [false, true]) {
        for (const development of [false, true]) {
            it(`should notify once after changed commits with strict=${strict} development=${development}`, () => {
                const result = render(tree(initial, strict, development));
                calls.length.should.equal(0);
                legacy.length.should.equal(1);
                result.rerender(tree({ value: { label: 'initial' } }, strict, development));
                calls.length.should.equal(0);
                legacy.length.should.equal(1);
                result.rerender(tree(changed, strict, development));
                calls.should.deep.equal([{ next: changed, previous: initial, text: 'changed' }]);
                // Legacy render-phase handling still repeats under StrictMode.
                legacy.should.deep.equal(strict ? [initial, changed, changed] : [initial, changed]);
                constructed.should.equal(1);
            });
        }
    }

    it('should start a new committed baseline after unmounting', async () => {
        const result = render(tree(initial, false, false));
        result.rerender(tree(changed, false, false));
        result.unmount();
        await flushDisposal();
        render(tree(changed, false, false));
        calls.length.should.equal(1);
        constructed.should.equal(2);
    });

    it('should notify a view model without legacy handling', () => {
        let count = 0;
        class HookOnly { propsChanged() { count++; } }
        const HookView = withViewModel<HookOnly, Props>(HookOnly, () => null);
        const result = render(createElement(MemoryRouter, null, createElement(HookView, initial)));
        result.rerender(createElement(MemoryRouter, null, createElement(HookView, changed)));
        count.should.equal(1);
    });

    it('should not notify or advance the baseline for an abandoned render', async () => {
        const notifications: [Props, Props][] = [];
        const viewModel = { propsChanged: (next: Props, previous: Props) => notifications.push([next, previous]) };
        const pending = new Promise<never>(() => { /* Suspends until this render is abandoned. */ });
        const result = renderHook(({ props, suspend }) => {
            usePropsChanged(viewModel, props);
            if (suspend) throw pending;
        }, { initialProps: { props: initial, suspend: false }, wrapper: ({ children }) => createElement(Suspense, { fallback: null }, children) });
        await act(async () => { result.rerender({ props: changed, suspend: true }); });
        notifications.length.should.equal(0);
        result.rerender({ props: { value: { label: 'committed' } }, suspend: false });
        notifications[0][1].should.deep.equal(initial);
    });
});
