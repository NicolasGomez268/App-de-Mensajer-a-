import { supabase } from './supabase';

const MENSAJES_API_URL = import.meta.env.VITE_MENSAJES_API_URL || 'http://localhost:5157';

export interface Mensaje {
  id: string;
  remitenteId: string;
  remitenteNombre?: string;
  remitenteAvatar?: string;
  destinatarioId: string;
  grupoId?: string;
  contenido: string;
  tipoMensaje: string;
  fechaEnvio: string;
  leido: boolean;
  fechaLectura?: string;
  archivoUrl?: string;
}

export interface Conversacion {
  id: string;
  otroUsuarioId: string;
  otroUsuarioNombre: string;
  otroUsuarioAvatar?: string;
  otroUsuarioEstado?: string;
  ultimaActividad: string;
  ultimoMensaje?: Mensaje;
  mensajesNoLeidos: number;
}

export interface EnviarMensajeRequest {
  destinatarioId: string;
  grupoId?: string;
  contenido: string;
  tipoMensaje?: string;
  archivoUrl?: string;
}

class MessagesApiClient {
  private async getAuthToken(): Promise<string | null> {
    const { data } = await supabase.auth.getSession();
    return data.session?.access_token || null;
  }

  async enviarMensaje(request: EnviarMensajeRequest): Promise<Mensaje> {
    const token = await this.getAuthToken();
    
    if (!token) {
      throw new Error('No authentication token available');
    }

    const response = await fetch(`${MENSAJES_API_URL}/api/messages`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify(request)
    });

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`Failed to send message: ${response.status} ${errorText}`);
    }

    return response.json();
  }

  async obtenerConversaciones(): Promise<Conversacion[]> {
    const token = await this.getAuthToken();
    
    if (!token) {
      throw new Error('No authentication token available');
    }

    const response = await fetch(`${MENSAJES_API_URL}/api/messages/conversations`, {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    });

    if (!response.ok) {
      throw new Error('Failed to get conversations');
    }

    return response.json();
  }

  async obtenerMensajes(otroUsuarioId: string, limit = 50): Promise<Mensaje[]> {
    const token = await this.getAuthToken();
    
    if (!token) {
      throw new Error('No authentication token available');
    }

    const response = await fetch(
      `${MENSAJES_API_URL}/api/messages/conversation/${otroUsuarioId}?limit=${limit}`,
      {
        headers: {
          'Authorization': `Bearer ${token}`
        }
      }
    );

    if (!response.ok) {
      throw new Error('Failed to get messages');
    }

    return response.json();
  }

  async marcarComoLeido(mensajeId: string): Promise<void> {
    const token = await this.getAuthToken();
    
    if (!token) {
      throw new Error('No authentication token available');
    }

    const response = await fetch(
      `${MENSAJES_API_URL}/api/messages/${mensajeId}/read`,
      {
        method: 'PATCH',
        headers: {
          'Authorization': `Bearer ${token}`
        }
      }
    );

    if (!response.ok) {
      throw new Error('Failed to mark message as read');
    }
  }

  async marcarTodosComoLeidos(otroUsuarioId: string): Promise<void> {
    const token = await this.getAuthToken();
    
    if (!token) {
      throw new Error('No authentication token available');
    }

    const response = await fetch(
      `${MENSAJES_API_URL}/api/messages/conversation/${otroUsuarioId}/read-all`,
      {
        method: 'PATCH',
        headers: {
          'Authorization': `Bearer ${token}`
        }
      }
    );

    if (!response.ok) {
      throw new Error('Failed to mark all messages as read');
    }
  }

  async eliminarMensaje(mensajeId: string): Promise<void> {
    const token = await this.getAuthToken();
    
    if (!token) {
      throw new Error('No authentication token available');
    }

    const response = await fetch(
      `${MENSAJES_API_URL}/api/messages/${mensajeId}`,
      {
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${token}`
        }
      }
    );

    if (!response.ok) {
      throw new Error('Failed to delete message');
    }
  }
}

export const messagesApiClient = new MessagesApiClient();
