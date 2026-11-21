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
                const equalContents: Props = { value: { label: 'initial' } };
                result.rerender(tree(equalContents, strict, development));
                calls.should.deep.equal([{ next: equalContents, previous: initial, text: 'initial' }]);
                legacy.length.should.equal(1);
                result.rerender(tree(changed, strict, development));
                calls.should.deep.equal([
                    { next: equalContents, previous: initial, text: 'initial' },
                    { next: changed, previous: equalContents, text: 'changed' },
                ]);
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

describe('when comparing committed prop identities', () => {
    const sharedObject = { label: 'same' };
    const sharedArray = ['same'];
    const sharedHandler = () => 'same';
    const symbolKey = Symbol('prop');
    const cases: { name: string; previous: object; next: object; notifications: number }[] = [
        { name: 'unchanged NaN', previous: { value: NaN }, next: { value: NaN }, notifications: 0 },
        { name: 'positive to negative zero', previous: { value: +0 }, next: { value: -0 }, notifications: 1 },
        { name: 'an added undefined key', previous: { value: 'same' }, next: { value: 'same', extra: undefined }, notifications: 1 },
        { name: 'a removed undefined key', previous: { value: 'same', extra: undefined }, next: { value: 'same' }, notifications: 1 },
        { name: 'a replaced undefined key', previous: { first: undefined }, next: { second: undefined }, notifications: 1 },
        { name: 'a new handler function', previous: { handler: () => 'same' }, next: { handler: () => 'same' }, notifications: 1 },
        {
            name: 'the same object array and handler references',
            previous: { object: sharedObject, array: sharedArray, handler: sharedHandler },
            next: { object: sharedObject, array: sharedArray, handler: sharedHandler },
            notifications: 0,
        },
        { name: 'a new equal-content array', previous: { value: ['same'] }, next: { value: ['same'] }, notifications: 1 },
        { name: 'a changed enumerable symbol key', previous: { [symbolKey]: 1 }, next: { [symbolKey]: 2 }, notifications: 1 },
    ];
    afterEach(() => cleanup());

    for (const scenario of cases) {
        describe(`with ${scenario.name}`, () => {
            let count: number;
            beforeEach(() => {
                count = 0;
                const viewModel = { propsChanged: () => { count++; } };
                const result = renderHook((props: object) => usePropsChanged(viewModel, props), { initialProps: scenario.previous });
                result.rerender(scenario.next);
            });
            it('should notify only for a shallow identity change', () => count.should.equal(scenario.notifications));
        });
    }

    describe('with a mutated props object reused on a later commit', () => {
        let previousProps: { value: object; label: string };
        let nextProps: { value: object; label: string };
        let count: number;
        beforeEach(() => {
            count = 0;
            const props = { value: sharedObject, label: 'before' };
            const viewModel = {
                propsChanged(next: typeof props, previous: typeof props) {
                    count++;
                    nextProps = next;
                    previousProps = previous;
                },
            };
            const result = renderHook((props: typeof nextProps) => usePropsChanged(viewModel, props), { initialProps: props });
            props.label = 'after';
            result.rerender(props);
        });
        it('should compare against a committed shallow snapshot', () => previousProps.label.should.equal('before'));
        it('should preserve nested value identities in the snapshot', () => previousProps.value.should.equal(sharedObject));
        it('should provide the newly committed value', () => nextProps.label.should.equal('after'));
        it('should notify once even when the props object reference is unchanged', () => count.should.equal(1));
    });
});
