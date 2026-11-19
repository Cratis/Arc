// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandResult } from '../CommandResult.js';

describe('when the command is forbidden for another reason', () => {
    const result = new CommandResult({
        correlationId: '00000000-0000-0000-0000-000000000000',
        isSuccess: false,
        isAuthorized: false,
        isValid: true,
        hasExceptions: false,
        validationResults: [],
        exceptionMessages: [],
        exceptionStackTrace: '',
        authorizationFailureReason: 'ModuleNotEntitled',
        response: null
    }, Object, false);
    let notAvailableCalled = false;
    result.onNotAvailable(() => notAvailableCalled = true);

    it('should not say it is not available', () => result.isNotAvailable.should.be.false);
    it('should not call the not available callback', () => notAvailableCalled.should.be.false);
});
