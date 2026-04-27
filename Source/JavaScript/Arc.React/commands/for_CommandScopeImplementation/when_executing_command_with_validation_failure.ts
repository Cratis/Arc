// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandResult } from '@cratis/arc/commands';
import { CommandScopeImplementation } from '../CommandScopeImplementation';
import { FakeCommand } from './FakeCommand';

describe('when executing command with validation failure', async () => {
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

    it('should have validation failures', () => scope.hasValidationFailures.should.be.true);
    it('should not have exceptions', () => scope.hasExceptions.should.be.false);
    it('should track validation failures for the command', () => scope.validationFailures.has(command).should.be.true);
    it('should store the correct number of validation results', () => scope.validationFailures.get(command)!.length.should.equal(1));
    it('should store the validation result message', () => scope.validationFailures.get(command)![0].message.should.equal('Name is required'));
    it('should expose one aggregated validation failure', () => scope.aggregatedValidationFailures.length.should.equal(1));
    it('should expose the validation failure in aggregated validation failures', () => scope.aggregatedValidationFailures[0].message.should.equal('Name is required'));
});
