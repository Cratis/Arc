// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { transform, wrapped } from './given.js';

describe('when adding to exported components', () => {
    it('should wrap what an exported function component returns', () =>
        transform('export function Pay() {\n    return <div />;\n}')!.should.contain(`return ${wrapped('<div />')};`));
    it('should wrap the body of an exported arrow component', () =>
        transform('export const Pay = () => <div />;')!.should.contain(`export const Pay = () => ${wrapped('<div />')};`));
    it('should wrap every return, including early ones', () => {
        const code = transform('export const Pay = (props: { show: boolean }) => {\n    if (!props.show) return null;\n    return <div />;\n};')!;
        code.should.contain(`return ${wrapped('null')};`);
        code.should.contain(`return ${wrapped('<div />')};`);
    });
    it('should wrap a default exported anonymous component', () =>
        transform('export default () => <div />;')!.should.contain(`export default () => ${wrapped('<div />')};`));
    it('should wrap a component exported by name later', () =>
        transform('function Pay() { return <div />; }\nexport { Pay };')!.should.contain(`return ${wrapped('<div />')};`));
    it('should wrap a component exported as default by name', () =>
        transform('const Pay = () => <div />;\nexport default Pay;')!.should.contain(`const Pay = () => ${wrapped('<div />')};`));
    it('should wrap a component given to a call such as withViewModel', () =>
        transform('export const Pay = withViewModel(PayViewModel, ({ viewModel }) => <div>{viewModel.name}</div>);')!.should.contain(wrapped('<div>{viewModel.name}</div>')));
    it('should wrap a component given to nested calls', () =>
        transform('export const Pay = memo(forwardRef((props, ref) => <div ref={ref} />));')!.should.contain(wrapped('<div ref={ref} />')));
    it('should wrap a component compiled to element factory calls', () =>
        transform('import { jsx as _jsx } from "react/jsx-runtime";\nexport const Pay = () => _jsx("div", {});', '/app/Pay.jsx')!.should.contain(wrapped('_jsx("div", {})')));
});
