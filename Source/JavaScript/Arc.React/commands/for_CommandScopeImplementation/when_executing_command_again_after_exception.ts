// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { CommandResult } from '@cratis/arc/commands';
import { CommandScopeImplementation } from '../CommandScopeImplementation';
import { FakeCommand } from './FakeCommand';

describe('when executing command again after exception', async () => {
    const exceptionResult = new CommandResult({
        correlationId: '00000000-0000-0000-0000-000000000000',
        isSuccess: false,
        isAuthorized: true,
        isValid: true,
        hasExceptions: true,
        validationResults: [],
        exceptionMessages: ['Something went wrong'],
        exceptionStackTrace: 'Stack trace here',
        authorizationFailureReason: '',
        response: {}
    }, Object, false);

    const scope = new CommandScopeImplementation(() => {});
    const command = new FakeCommand(true, exceptionResult);
    scope.addCommand(command);

    await scope.execute();

    // Re-execute: command reports hasChanges again and succeeds this time
    command['_hasChanges'] = true;
    command.execute = sinon.fake(() => Promise.resolve(CommandResult.empty));

    await scope.execute();

    it('should not have exceptions after successful re-execution', () => scope.hasExceptions.should.be.false);
    it('should not track exceptions for the command', () => scope.exceptions.has(command).should.be.false);
    it('should not expose aggregated exceptions after successful re-execution', () => scope.aggregatedExceptions.length.should.equal(0));
});
