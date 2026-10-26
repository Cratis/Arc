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
});
