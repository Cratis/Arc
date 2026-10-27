// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { transform } from './given.js';

const isUntouched = (code: string) => (transform(code) === undefined).should.be.true;

describe('when adding to a module without exported components', () => {
    it('should leave a module exporting only lowercase functions alone', () => isUntouched('export const renderRow = () => <tr />;'));
    it('should leave a module whose exported uppercase function renders no JSX alone', () => isUntouched('export const Total = (items: number[]) => items.length;'));
    it('should leave a component that is not exported alone', () => isUntouched('const Row = () => <tr />;\nexport const rows = 1;'));
    it('should leave an exported class alone', () => isUntouched('export class Pay { render() { return <div />; } }'));
    it('should leave a module that does not parse alone', () => isUntouched('export const Pay = () => <div>;'));
    it('should leave a module it already changed alone', () => isUntouched(transform('export const Pay = () => <div />;')!));
    it('should leave a store whose state holds a function rendering JSX alone', () =>
        isUntouched('export const Store = create((set) => ({ render: () => <div />, count: 0 }));'));
    it('should leave a function returning columns with JSX renderers alone', () =>
        isUntouched('export function Columns() {\n    return [{ field: \'a\', body: (row) => <span>{row.a}</span> }];\n}'));
    it('should leave a function that returns JSX only from a nested function alone', () =>
        isUntouched('export function Renderer() {\n    const render = () => <div />;\n    return render;\n}'));
    it('should leave a function returning JSX and something else alone', () =>
        isUntouched('export function Format(props: { raw: boolean }) {\n    if (props.raw) return \'text\';\n    return <span />;\n}'));
    it('should leave an async function returning JSX alone', () => isUntouched('export async function Load() { return <div />; }'));
    it('should leave a function returning only null alone', () => isUntouched('export function Nothing() { return null; }'));
});
