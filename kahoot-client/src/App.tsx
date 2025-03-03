import React from 'react';
import './App.css';
import { WebSocketProvider } from './WebSocketContext';
import { Game } from './components/Game';

function App() {
    return (
        <WebSocketProvider>
            <Game />
        </WebSocketProvider>
    );
}

export default App;