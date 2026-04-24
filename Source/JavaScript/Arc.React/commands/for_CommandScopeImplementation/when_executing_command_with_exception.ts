// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandResult } from '@cratis/arc/commands';
import { CommandScopeImplementation } from '../CommandScopeImplementation';
import { FakeCommand } from './FakeCommand';

describe('when executing command with exception', async () => {
    const exceptionResult = new CommandResult({
        correlationId: '00000000-0000-0000-0000-000000000000',
        isSuccess: false,
        isAuthorized: true,
        isValid: true,
        hasExceptions: true,
        validationResults: [],
        exceptionMessages: ['Something went wrong', 'Another error'],
        exceptionStackTrace: 'Stack trace here',
        authorizationFailureReason: '',
        response: {}
    }, Object, false);

    const scope = new CommandScopeImplementation(() => {});
    const command = new FakeCommand(true, exceptionResult);
    scope.addCommand(command);

    await scope.execute();

    it('should have exceptions', () => scope.hasExceptions.should.be.true);
    it('should not have validation failures', () => scope.hasValidationFailures.should.be.false);
    it('should track exceptions for the command', () => scope.exceptions.has(command).should.be.true);
    it('should store the correct number of exception messages', () => scope.exceptions.get(command)!.length.should.equal(2));
    it('should store the first exception message', () => scope.exceptions.get(command)![0].should.equal('Something went wrong'));
    it('should store the second exception message', () => scope.exceptions.get(command)![1].should.equal('Another error'));
});
