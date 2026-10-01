// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useRef } from 'react';
import * as monaco from 'monaco-editor';
import EditorWorker from 'monaco-editor/editor/editor.worker?worker';
import { languageId, register, screenplayDarkThemeName } from '@cratis/screenplay-language';

// Monaco runs its language work in a worker, bundled with the viewer rather than fetched from a CDN, so the
// explorer works wherever the application runs.
self.MonacoEnvironment = {
    getWorker: () => new EditorWorker()
};
register(monaco);

export interface ScreenplayEditorProps {
    readonly value: string;
}

/**
 * Screenplay source in Monaco, highlighted by the Screenplay language service the way the Screenplay
 * editor and Chronicle show it. The source is generated from compiled code, so it is read-only.
 */
export const ScreenplayEditor = ({ value }: ScreenplayEditorProps) => {
    const containerRef = useRef<HTMLDivElement>(null);
    const editorRef = useRef<monaco.editor.IStandaloneCodeEditor | null>(null);

    useEffect(() => {
        if (!containerRef.current) return;
        // Created empty once; the effect below gives it the source and keeps it current.
        const editor = monaco.editor.create(containerRef.current, {
            language: languageId,
            theme: screenplayDarkThemeName,
            readOnly: true,
            domReadOnly: true,
            automaticLayout: true,
            fontSize: 13,
            tabSize: 2,
            minimap: { enabled: true },
            scrollBeyondLastLine: false,
            fixedOverflowWidgets: true,
            renderLineHighlight: 'none'
        });
        editorRef.current = editor;
        return () => {
            editor.getModel()?.dispose();
            editor.dispose();
            editorRef.current = null;
        };
    }, []);

    useEffect(() => {
        const editor = editorRef.current;
        if (editor && editor.getValue() !== value) editor.setValue(value);
    }, [value]);

    return <div className='source-editor' ref={containerRef} />;
};

export default ScreenplayEditor;
