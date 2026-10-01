// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ScreenplayEditorProps } from '../../ScreenplayEditor';

// Monaco cannot run in jsdom; the stand-in shows the value it was given.
export const ScreenplayEditor = ({ value }: ScreenplayEditorProps) => <pre data-testid='screenplay-editor'>{value}</pre>;

export default ScreenplayEditor;
