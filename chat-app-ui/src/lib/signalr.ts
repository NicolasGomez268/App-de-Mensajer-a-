import * as signalR from '@microsoft/signalr';
import { supabase } from './supabase';

const MENSAJES_API_URL = import.meta.env.VITE_MENSAJES_API_URL || 'http://localhost:5157';

let connection: signalR.HubConnection | null = null;

export const createSignalRConnection = async () => {
  if (connection) {
    return connection;
  }

  // Obtener token de Supabase
  const { data: { session } } = await supabase.auth.getSession();
  if (!session?.access_token) {
    throw new Error('No hay sesión activa');
  }

  connection = new signalR.HubConnectionBuilder()
    .withUrl(`${MENSAJES_API_URL}/hubs/chat`, {
      accessTokenFactory: () => session.access_token,
      skipNegotiation: true,
      transport: signalR.HttpTransportType.WebSockets
    })
    .withAutomaticReconnect({
      nextRetryDelayInMilliseconds: () => 3000 // Reintentar cada 3 segundos
    })
    .configureLogging(signalR.LogLevel.Information)
    .build();

  return connection;
};

export const startConnection = async () => {
  if (!connection) {
    connection = await createSignalRConnection();
  }

  if (connection.state === signalR.HubConnectionState.Disconnected) {
    try {
      await connection.start();
      console.log('✅ SignalR Connected');
    } catch (error) {
      console.error('❌ SignalR Connection Error:', error);
      throw error;
    }
  }

  return connection;
};

export const stopConnection = async () => {
  if (connection && connection.state === signalR.HubConnectionState.Connected) {
    await connection.stop();
    console.log('🔌 SignalR Disconnected');
  }
  connection = null;
};

export const getConnection = () => connection;

export { signalR };

