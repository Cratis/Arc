// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { deferredReactionScheduler } from '../deferredReactionScheduler';

describe('when scheduling a reaction', () => {
    let ranBeforeMicrotask: boolean;
    let ran: boolean;

    beforeEach(async () => {
        ran = false;
        deferredReactionScheduler(() => { ran = true; });
        ranBeforeMicrotask = ran;
        await Promise.resolve();
    });

    it('should not run the reaction synchronously', () => ranBeforeMicrotask.should.be.false);
    it('should run the reaction on the next microtask', () => ran.should.be.true);
});
