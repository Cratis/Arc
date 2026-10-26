// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SliceLocation } from '@cratis/arc/reflection';
import { SliceScope } from '../SliceScope.js';

describe('when registering an empty location', () => {
    let notifications: number;

    beforeEach(() => {
        const scope = new SliceScope();
        notifications = 0;
        scope.subscribe(() => notifications++);
        scope.register(SliceLocation.empty)();
    });

    it('should not notify', () => notifications.should.equal(0));
});
