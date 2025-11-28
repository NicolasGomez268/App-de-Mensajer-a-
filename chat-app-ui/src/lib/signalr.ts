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
  console.log('[SignalR] Sesión:', session);
  if (!session?.access_token) {
    console.error('[SignalR] No hay sesión activa, no se puede conectar');
    throw new Error('No hay sesión activa');
  }
  console.log('[SignalR] Token JWT:', session.access_token);
  console.log('[SignalR] User ID (sub):', session.user?.id);

  connection = new signalR.HubConnectionBuilder()
    .withUrl(`${MENSAJES_API_URL}/hubs/chat`, {
      accessTokenFactory: () => {
        console.log('[SignalR] accessTokenFactory called, token:', session.access_token);
        return session.access_token;
      },
      skipNegotiation: true,
      transport: signalR.HttpTransportType.WebSockets
    })
    .withAutomaticReconnect({
      nextRetryDelayInMilliseconds: () => 3000 // Reintentar cada 3 segundos
    })
    .configureLogging(signalR.LogLevel.Information)
    .build();

  console.log('[SignalR] HubConnection creado:', connection);
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

