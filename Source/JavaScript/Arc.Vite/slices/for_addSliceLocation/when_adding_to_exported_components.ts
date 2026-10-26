// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { transform, wrapped } from './given.js';

describe('when adding to exported components', () => {
    it('should wrap an exported function component at its declaration', () =>
        transform('export function Pay() {\n    return <div />;\n}')!.should.contain(`export const Pay = ${wrapped('function Pay() {\n    return <div />;\n}', 'Pay')};`));
    it('should wrap an exported arrow component', () =>
        transform('export const Pay = () => <div />;')!.should.contain(`export const Pay = ${wrapped('() => <div />', 'Pay')};`));
    it('should leave what the component returns alone', () =>
        transform('export const Pay = (props: { show: boolean }) => {\n    if (!props.show) return null;\n    return <div />;\n};')!.should.contain('if (!props.show) return null;\n    return <div />;'));
    it('should wrap a default exported anonymous arrow component', () =>
        transform('export default () => <div />;')!.should.contain(`export default ${wrapped('() => <div />')};`));
    it('should wrap a default exported named function component and keep its name', () =>
        transform('export default function Pay() { return <div />; }')!.should.contain(`const Pay = ${wrapped('function Pay() { return <div />; }', 'Pay')};\nexport default Pay;`));
    it('should wrap a default exported anonymous function component', () =>
        transform('export default function () { return <div />; }')!.should.contain(`export default ${wrapped('function () { return <div />; }')};`));
    it('should wrap a component exported by name later at its declaration', () =>
        transform('function Pay() { return <div />; }\nexport { Pay };')!.should.contain(`const Pay = ${wrapped('function Pay() { return <div />; }', 'Pay')};\nexport { Pay };`));
    it('should wrap a component exported as default by name at its declaration', () =>
        transform('const Pay = () => <div />;\nexport default Pay;')!.should.contain(`const Pay = ${wrapped('() => <div />', 'Pay')};`));
    it('should wrap the whole call for a component given to withViewModel', () =>
        transform('export const Pay = withViewModel(PayViewModel, ({ viewModel }) => <div>{viewModel.name}</div>);')!
            .should.contain(`export const Pay = ${wrapped('withViewModel(PayViewModel, ({ viewModel }) => <div>{viewModel.name}</div>)', 'Pay')};`));
    it('should wrap the whole call for a memo component', () =>
        transform('export const Pay = memo(function Pay() { return <div />; });')!.should.contain(`export const Pay = ${wrapped('memo(function Pay() { return <div />; })', 'Pay')};`));
    it('should wrap the whole call for a forward ref component', () =>
        transform('export const Pay = forwardRef((props, ref) => <div ref={ref} />);')!.should.contain(`export const Pay = ${wrapped('forwardRef((props, ref) => <div ref={ref} />)', 'Pay')};`));
    it('should wrap a call given a top-level component by name', () =>
        transform('function PayInner() { return <div />; }\nexport const Pay = memo(PayInner);')!.should.contain(`export const Pay = ${wrapped('memo(PayInner)', 'Pay')};`));
    it('should wrap a component compiled to element factory calls', () =>
        transform('import { jsx as _jsx } from "react/jsx-runtime";\nexport const Pay = () => _jsx("div", {});', '/app/Pay.jsx')!.should.contain(wrapped('() => _jsx("div", {})', 'Pay')));
    it('should wrap a component returning JSX held in a constant', () =>
        transform('export function Pay() {\n    const content = <div />;\n    return content;\n}')!.should.contain('export const Pay = __cratisWithSliceLocation(function Pay()'));
    it('should wrap a component returning conditional and logical JSX', () =>
        transform('export const Pay = (props: { show: boolean, busy: boolean }) => props.busy ? null : (props.show && <div />);')!.should.contain('export const Pay = __cratisWithSliceLocation('));
});
