// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ApplicationContext } from '../ApplicationContext.js';

describe('when contributing', () => {
    let context: ApplicationContext;
    let withContribution: ReturnType<ApplicationContext['getSnapshot']>;
    let afterRemoval: ReturnType<ApplicationContext['getSnapshot']>;

    beforeEach(() => {
        context = new ApplicationContext();
        context.contribute({ screen: { id: 'orders', name: 'Orders' }, values: { version: '1.0', theme: 'dark' } });
        const second = context.contribute({ screen: { path: '/orders' }, values: { version: '2.0' } });
        withContribution = context.getSnapshot();
        second.remove();
        afterRemoval = context.getSnapshot();
    });

    it('should merge the screen', () => withContribution.screen.should.deep.equal({ id: 'orders', name: 'Orders', path: '/orders' }));
    it('should let later values win', () => withContribution.contributions.version!.should.equal('2.0'));
    it('should keep other values', () => withContribution.contributions.theme!.should.equal('dark'));
    it('should drop removed contributions', () => {
        afterRemoval.screen.path.should.equal('');
        afterRemoval.contributions.version!.should.equal('1.0');
    });
});
