// WebSocket polyfill for test environment
class WebSocket {
    constructor(url) {
        this.url = url;
        this.readyState = 0; // CONNECTING
        this.onopen = null;
        this.onmessage = null;
        this.onerror = null;
        this.onclose = null;
        this._id = __createWebSocket(url);
        
        // Store reference so C# can call callbacks
        if (!globalThis.__webSockets) {
            globalThis.__webSockets = {};
        }
        globalThis.__webSockets[this._id] = this;
    }
    
    send(data) {
        if (this.readyState !== 1) { // OPEN
            throw new Error('WebSocket is not open');
        }
        __webSocketSend(this._id, typeof data === 'string' ? data : JSON.stringify(data));
    }
    
    close() {
        if (this.readyState === 2 || this.readyState === 3) { // CLOSING or CLOSED
            return;
        }
        this.readyState = 2; // CLOSING
        __webSocketClose(this._id);
    }
}

// WebSocket constants
WebSocket.CONNECTING = 0;
WebSocket.OPEN = 1;
WebSocket.CLOSING = 2;
WebSocket.CLOSED = 3;
