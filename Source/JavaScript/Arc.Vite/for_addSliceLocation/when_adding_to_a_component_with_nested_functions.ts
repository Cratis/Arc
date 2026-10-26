// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { transform, wrapped } from './given.js';

describe('when adding to a component with nested functions', () => {
    const code = transform(`export const Listing = (props: { items: string[] }) => {
    const toItem = (item: string) => {
        return <li>{item}</li>;
    };
    return <ul>{props.items.map(toItem)}</ul>;
};`)!;

    it('should wrap what the component returns', () => code.should.contain(`return ${wrapped('<ul>{props.items.map(toItem)}</ul>')};`));
    it('should not wrap what a nested function returns', () => code.should.contain('return <li>{item}</li>;'));
});
