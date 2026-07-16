// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { deferredReactionScheduler } from '../deferredReactionScheduler';

describe('when a reaction schedules another reaction during flush', () => {
    let outerRan: boolean;
    let followUpRan: boolean;

    beforeEach(async () => {
        outerRan = false;
        followUpRan = false;
        deferredReactionScheduler(() => {
            outerRan = true;
            deferredReactionScheduler(() => { followUpRan = true; });
        });
        await Promise.resolve();
        await Promise.resolve();
    });

    it('should run the outer reaction', () => outerRan.should.be.true);
    it('should run the follow-up reaction scheduled during the flush', () => followUpRan.should.be.true);
});
