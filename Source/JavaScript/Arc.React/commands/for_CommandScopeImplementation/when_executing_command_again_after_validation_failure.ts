// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { CommandResult } from '@cratis/arc/commands';
import { CommandScopeImplementation } from '../CommandScopeImplementation';
import { FakeCommand } from './FakeCommand';

describe('when executing command again after validation failure', async () => {
    const failedResult = new CommandResult({
        correlationId: '00000000-0000-0000-0000-000000000000',
        isSuccess: false,
        isAuthorized: true,
        isValid: false,
        hasExceptions: false,
        validationResults: [{ severity: 0, message: 'Name is required', members: ['name'], state: {} }],
        exceptionMessages: [],
        exceptionStackTrace: '',
        authorizationFailureReason: '',
        response: {}
    }, Object, false);

    const scope = new CommandScopeImplementation(() => {});
    const command = new FakeCommand(true, failedResult);
    scope.addCommand(command);

    await scope.execute();

    // Re-execute: command reports hasChanges again (simulating user correcting input)
    command['_hasChanges'] = true;
    command.execute = sinon.fake(() => Promise.resolve(CommandResult.empty));

    await scope.execute();

    it('should not have validation failures after successful re-execution', () => scope.hasValidationFailures.should.be.false);
    it('should not track validation failures for the command', () => scope.validationFailures.has(command).should.be.false);
});
