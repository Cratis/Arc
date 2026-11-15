// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Command } from '../Command.js';
import { CommandResult } from '../CommandResult.js';
import { CommandResults } from '../CommandResults.js';

const aggregate = (missing: boolean) => new CommandResults(new Map([
    [{} as Command, CommandResult.empty],
    [{} as Command, Object.assign(CommandResult.failed(['diagnostic']), { isMissingHandler: missing })]
]));

describe('when aggregating missing handler results', () => {
    it('should report a missing handler when any member does', () => aggregate(true).isMissingHandler.should.be.true);
    it('should not report a missing handler for other failures', () => aggregate(false).isMissingHandler.should.be.false);
    it('should not report a missing handler for no results', () => new CommandResults(new Map()).isMissingHandler.should.be.false);
});
