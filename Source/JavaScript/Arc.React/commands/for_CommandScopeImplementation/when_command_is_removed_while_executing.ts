// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { CommandResult } from '@cratis/arc/commands';
import { CommandScopeImplementation } from '../CommandScopeImplementation.js';
import { FakeCommand } from './FakeCommand.js';

describe('when command is removed while executing', () => {
    it('should return its result and notify callbacks without restoring its failure state', async () => {
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
        const callbacks = { onFailed: sinon.spy(), onValidationFailure: sinon.spy(), onException: sinon.spy() };
        const scope = new CommandScopeImplementation(() => {}, undefined, undefined, () => callbacks);
        const command = new FakeCommand(true);
        let complete!: (result: CommandResult) => void;
        command.execute = sinon.fake(() => new Promise<CommandResult>(resolve => { complete = resolve; }));
        scope.addCommand(command);

        const execution = scope.execute();
        scope.removeCommand(command);
        complete(result);
        const results = await execution;

        results.isSuccess.should.be.false;
        results.validationResults.should.deep.equal(result.validationResults);
        results.exceptionMessages.should.deep.equal(result.exceptionMessages);
        scope.validationFailures.size.should.equal(0);
        scope.exceptions.size.should.equal(0);
        scope.hasValidationFailures.should.be.false;
        scope.hasExceptions.should.be.false;
        scope.hasChanges.should.be.false;
        callbacks.onFailed.calledOnceWith(command, result).should.be.true;
        callbacks.onValidationFailure.calledOnceWith(command, result).should.be.true;
        callbacks.onException.calledOnceWith(command, result).should.be.true;
    });
});
