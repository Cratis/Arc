// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { joinPaths } from '@cratis/arc';
import { Arc } from '@cratis/arc.react';
import { observer } from '@cratis/arc.react.mvvm';
import { EmitMetadataPlugin } from '@cratis/arc.vite';
import * as arcCommands from '@cratis/arc/commands';
import * as arcIdentity from '@cratis/arc/identity';
import * as arcMessaging from '@cratis/arc/messaging';
import * as arcQueries from '@cratis/arc/queries';
import * as arcValidation from '@cratis/arc/validation';
import * as arcReflection from '@cratis/arc/reflection';
import * as reactCommands from '@cratis/arc.react/commands';
import * as reactQueries from '@cratis/arc.react/queries';
import * as reactDialogs from '@cratis/arc.react/dialogs';
import * as reactMessaging from '@cratis/arc.react/messaging';
import * as reactIdentity from '@cratis/arc.react/identity';
import * as reactStories from '@cratis/arc.react/stories';
import * as mvvmBrowser from '@cratis/arc.react.mvvm/browser';
import * as mvvmMessaging from '@cratis/arc.react.mvvm/messaging';
import * as mvvmDialogs from '@cratis/arc.react.mvvm/dialogs';

void [joinPaths, Arc, observer, EmitMetadataPlugin, arcCommands, arcIdentity, arcMessaging,
    arcQueries, arcValidation, arcReflection, reactCommands, reactQueries, reactDialogs,
    reactMessaging, reactIdentity, reactStories, mvvmBrowser, mvvmMessaging, mvvmDialogs];
