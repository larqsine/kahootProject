import React, { createContext, useContext, useEffect, useState } from 'react';
import { WebSocketMessage } from './types';

interface WebSocketContextType {
  sendMessage: (message: any) => void;
  lastMessage: WebSocketMessage | null;
  connected: boolean;
}

const WebSocketContext = createContext<WebSocketContextType | undefined>(undefined);

export const WebSocketProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [socket, setSocket] = useState<WebSocket | null>(null);
  const [connected, setConnected] = useState(false);
  const [lastMessage, setLastMessage] = useState<WebSocketMessage | null>(null);

  useEffect(() => {
    const ws = new WebSocket('ws://localhost:8181');

    ws.onopen = () => {
      console.log('WebSocket Connected');
      setConnected(true);
    };

    ws.onclose = (event) => {
      console.log('WebSocket Disconnected:', event.code, event.reason);
      setConnected(false);
    };

    ws.onerror = (error) => {
      console.error('WebSocket Error:', error);
      setConnected(false);
    };

    ws.onmessage = (event) => {
      try {
        const message = JSON.parse(event.data);
        console.log('Received:', message);
        setLastMessage(message);
      } catch (error) {
        console.error('Error parsing message:', error);
      }
    };

    setSocket(ws);

    return () => {
      console.log('Cleaning up WebSocket');
      ws.close();
    };
  }, []);

  const sendMessage = (message: any) => {
    if (socket?.readyState === WebSocket.OPEN) {
      console.log('Sending:', message);
      socket.send(JSON.stringify(message));
    } else {
      console.warn('WebSocket is not connected');
    }
  };

  return (
      <WebSocketContext.Provider value={{ sendMessage, lastMessage, connected }}>
        {children}
      </WebSocketContext.Provider>
  );
};

export const useWebSocket = () => {
  const context = useContext(WebSocketContext);
  if (!context) {
    throw new Error('useWebSocket must be used within a WebSocketProvider');
  }
  return context;
};