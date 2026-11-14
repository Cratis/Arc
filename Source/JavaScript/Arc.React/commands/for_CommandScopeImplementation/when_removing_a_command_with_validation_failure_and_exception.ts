// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandResult } from '@cratis/arc/commands';
import { CommandScopeImplementation } from '../CommandScopeImplementation.js';
import { FakeCommand } from './FakeCommand.js';

describe('when removing a command with validation failure and exception', () => {
    it('should discard its failures without discarding another command failures', async () => {
        const result = new CommandResult({
            correlationId: '00000000-0000-0000-0000-000000000000',
            isSuccess: false,
            isAuthorized: true,
            isValid: false,
            hasExceptions: true,
            validationResults: [{ severity: 0, message: 'Name is required', members: ['name'], state: {} }],
            exceptionMessages: ['Something went wrong'],
            exceptionStackTrace: '',
            authorizationFailureReason: '',
            response: {}
        }, Object, false);
        const scope = new CommandScopeImplementation(() => {});
        const removed = new FakeCommand(true, result);
        const remaining = new FakeCommand(true, result);
        scope.addCommand(removed);
        scope.addCommand(remaining);
        await scope.execute();
        scope.validationFailures.size.should.equal(2);
        scope.exceptions.size.should.equal(2);

        scope.removeCommand(removed);

        scope.validationFailures.has(removed).should.be.false;
        scope.exceptions.has(removed).should.be.false;
        scope.validationFailures.has(remaining).should.be.true;
        scope.exceptions.has(remaining).should.be.true;
        scope.removeCommand(remaining);
        scope.validationFailures.size.should.equal(0);
        scope.exceptions.size.should.equal(0);
        scope.hasValidationFailures.should.be.false;
        scope.hasExceptions.should.be.false;
        scope.aggregatedValidationFailures.should.be.empty;
        scope.aggregatedExceptions.should.be.empty;
    });
});
