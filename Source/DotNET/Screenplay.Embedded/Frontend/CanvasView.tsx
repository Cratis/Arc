// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {
    EventModelBoard,
    EventModelMessagesProvider,
    EventModelPresentationProvider,
    MenuDropdownOpenProvider,
    defaultEventModelMessages
} from '@cratis/event-models';
import { SceneMessagesProvider, defaultSceneMessages } from '@cratis/scene/Prototypes';
import { getModel } from './api';
import { boardChrome } from './boardChrome';
import { BoardErrorBoundary } from './BoardErrorBoundary';
import { defaultPresentation } from './presentation';
import type { DocumentSelection } from './Hierarchy';
import { defaultFitOptions } from './fitToContent';
import { useFitToModel } from './useFitToModel';
import { usePresentation } from './usePresentation';
import { useRemote } from './useRemote';
import { Status } from './Status';

/**
 * The published board, read-only, with the board's own view options in its upper right. Nothing here
 * can be edited and nothing but the view options is persisted: the document is generated from compiled source and the viewer only draws it. Opening a
 * document frames the whole model; after that the camera belongs to the person looking at it.
 */
export const CanvasView = ({ projectId, documentId }: DocumentSelection) => {
    const model = useRemote(
        signal => getModel(projectId, documentId, signal),
        [projectId, documentId]);
    const { presentation, change, storageFailure } = usePresentation();
    const { containerRef, onHandleReady } = useFitToModel(`${projectId}/${documentId}`);

    if (model.status === 'loading') return <Status kind='loading' message='Loading event model…' />;
    if (model.status === 'error') return <Status kind='error' message={`Could not load the event model. ${model.error.message}`} />;

    const { document, warnings } = model.value;

    return (
        <div className='canvas-view'>
            {warnings.length > 0 && (
                <ul className='warnings' aria-label='Event model warnings'>
                    {warnings.map((warning, index) => <li key={`${index}:${warning}`}>{warning}</li>)}
                </ul>
            )}
            {storageFailure && (
                <p className='view-options-failure' role='alert'>
                    Your view options are shown, but could not be remembered for the next visit. {storageFailure}
                </p>
            )}
            <EventModelMessagesProvider messages={defaultEventModelMessages}>
                <SceneMessagesProvider messages={defaultSceneMessages}>
                    <MenuDropdownOpenProvider>
                        <EventModelPresentationProvider presentation={presentation} onChange={next => change(() => next)}>
                            <div className='canvas event-modeling-board' data-event-model-board ref={containerRef}>
                                <BoardErrorBoundary resetWhenChanged={presentation} onReset={() => change(() => defaultPresentation)}>
                                    <EventModelBoard
                                        document={document}
                                        readOnly
                                        showViewOptions
                                        canvas={{
                                            chrome: boardChrome,
                                            controlsPlacement: 'bottom-right',
                                            disableControlsGlass: true,
                                            minZoom: defaultFitOptions.minZoom,
                                            onHandleReady
                                        }} />
                                </BoardErrorBoundary>
                            </div>
                        </EventModelPresentationProvider>
                    </MenuDropdownOpenProvider>
                </SceneMessagesProvider>
            </EventModelMessagesProvider>
        </div>
    );
};
