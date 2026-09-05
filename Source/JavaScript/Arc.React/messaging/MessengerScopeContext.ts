// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { IMessenger } from '@cratis/arc/messaging';

export const MessengerScopeContext = React.createContext<IMessenger | undefined>(undefined);
