// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createContext } from 'react';
import type { ICommandResult } from '@cratis/arc/commands';

// Private feedback source for per-field merges. The public result can describe a custom-error
// submission gate and must never be copied back into native validation feedback.
export const CommandFormNativeResultContext = createContext<{
    result: ICommandResult<unknown> | undefined;
} | undefined>(undefined);
