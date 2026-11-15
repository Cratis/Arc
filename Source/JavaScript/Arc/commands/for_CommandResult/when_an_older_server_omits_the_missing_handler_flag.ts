// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandResult } from '../CommandResult.js';

const result = new CommandResult({
    correlationId: '00000000-0000-0000-0000-000000000000',
    isSuccess: false,
    isAuthorized: true,
    isValid: true,
    hasExceptions: true,
    validationResults: [],
    exceptionMessages: ['No handler found for command of type Example'],
    exceptionStackTrace: '',
    authorizationFailureReason: '',
    response: null
}, Object, false);

describe('when an older server omits the missing handler flag', () => {
    it('should default the flag to false without interpreting the message', () => result.isMissingHandler.should.be.false);
    it('should preserve the exception outcome', () => result.hasExceptions.should.be.true);
});
