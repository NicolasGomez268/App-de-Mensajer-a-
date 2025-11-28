import { create } from 'zustand';
import { messagesApiClient, type Conversacion, type Mensaje } from '../lib/messagesApi';
import { getConnection, startConnection, stopConnection } from '../lib/signalr';

interface MessagesState {
  conversaciones: Conversacion[];
  conversacionActual: string | null;
  tipoConversacionActual: 'usuario' | 'grupo' | null;
  mensajes: Record<string, Mensaje[]>;
  usuariosEscribiendo: Set<string>;
  loading: boolean;
  error: string | null;

  // Actions
  inicializarSignalR: () => Promise<void>;
  detenerSignalR: () => Promise<void>;
  cargarConversaciones: () => Promise<void>;
  seleccionarConversacion: (id: string, tipo: 'usuario' | 'grupo') => Promise<void>;
  iniciarNuevaConversacion: (usuarioId: string) => Promise<void>;
  enviarMensaje: (destinatarioId: string, contenido: string, grupoId?: string) => Promise<void>;
  marcarComoLeido: (mensajeId: string) => Promise<void>;
  notificarEscribiendo: (destinatarioId: string) => void;
  notificarDejoDeEscribir: (destinatarioId: string) => void;
}

export const useMessagesStore = create<MessagesState>((set, get) => ({
  conversaciones: [],
  conversacionActual: null,
  tipoConversacionActual: null,
  mensajes: {},
  usuariosEscribiendo: new Set(),
  loading: false,
  error: null,

  inicializarSignalR: async () => {
    try {
      const connection = await startConnection();

      // Escuchar mensaje recibido
      connection.on('ReceiveMessage', (mensaje: any) => {
        console.log('📨 Mensaje recibido:', mensaje);

        const { mensajes, conversacionActual } = get();
        const chatKey = mensaje.grupoId || mensaje.remitenteId;

        // Agregar mensaje a la lista si no existe
        const mensajesChat = mensajes[chatKey] || [];
        const existe = mensajesChat.some(m => m.id === mensaje.id);

        if (!existe) {
          set({
            mensajes: {
              ...mensajes,
              [chatKey]: [...mensajesChat, {
                id: mensaje.id || crypto.randomUUID(),
                remitenteId: mensaje.remitenteId,
                remitenteNombre: mensaje.remitenteNombre,
                destinatarioId: mensaje.destinatarioId,
                grupoId: mensaje.grupoId,
                contenido: mensaje.contenido,
                tipoMensaje: mensaje.tipoMensaje || 'texto',
                fechaEnvio: mensaje.fechaEnvio || new Date().toISOString(),
                leido: false
              }]
            }
          });
        }

        // Si es la conversación actual, marcar como leído
        if (conversacionActual === chatKey) {
          if (mensaje.id) {
            messagesApiClient.marcarComoLeido(mensaje.id);
          }
        }

        // Recargar conversaciones para actualizar el último mensaje
        get().cargarConversaciones();
      });

      // Escuchar confirmación de envío
      connection.on('MessageSent', () => {
        console.log('✅ Mensaje enviado confirmado');
      });

      // Escuchar mensaje leído
      connection.on('MessageRead', (data: any) => {
        console.log('👁️ Mensaje leído:', data.mensajeId);

        const { mensajes } = get();
        const nuevosMensajes = { ...mensajes };

        // Actualizar estado de leído en todos los mensajes
        Object.keys(nuevosMensajes).forEach(usuarioId => {
          nuevosMensajes[usuarioId] = nuevosMensajes[usuarioId].map(m =>
            m.id === data.mensajeId ? { ...m, leido: true, fechaLectura: data.fechaLectura } : m
          );
        });

        set({ mensajes: nuevosMensajes });
      });

      // Escuchar usuario escribiendo
      connection.on('UserTyping', (usuarioId: string) => {
        const { usuariosEscribiendo } = get();
        set({ usuariosEscribiendo: new Set(usuariosEscribiendo).add(usuarioId) });
      });

      // Escuchar usuario dejó de escribir
      connection.on('UserStoppedTyping', (usuarioId: string) => {
        const { usuariosEscribiendo } = get();
        const nuevo = new Set(usuariosEscribiendo);
        nuevo.delete(usuarioId);
        set({ usuariosEscribiendo: nuevo });
      });

      // Escuchar usuario conectado
      connection.on('UserConnected', (usuarioId: string) => {
        console.log('🟢 Usuario conectado:', usuarioId);
        // Actualizar estado en conversaciones
        get().cargarConversaciones();
      });

      // Escuchar usuario desconectado
      connection.on('UserDisconnected', (usuarioId: string) => {
        console.log('🔴 Usuario desconectado:', usuarioId);
        // Actualizar estado en conversaciones
        get().cargarConversaciones();
      });

      console.log('✅ SignalR inicializado correctamente');
    } catch (error) {
      console.error('❌ Error al inicializar SignalR:', error);
      set({ error: 'Error al conectar con el servidor de mensajes' });
    }
  },

  detenerSignalR: async () => {
    await stopConnection();
  },

  cargarConversaciones: async () => {
    set({ loading: true, error: null });
    try {
      const conversaciones = await messagesApiClient.obtenerConversaciones();
      set({ conversaciones, loading: false });
    } catch (error) {
      console.error('Error al cargar conversaciones:', error);
      set({ error: 'Error al cargar conversaciones', loading: false });
    }
  },

  seleccionarConversacion: async (id: string, tipo: 'usuario' | 'grupo') => {
    set({ conversacionActual: id, tipoConversacionActual: tipo, loading: true, error: null });

    try {
      // Si es grupo, unirse a la sala de SignalR
      if (tipo === 'grupo') {
        const connection = getConnection();
        if (connection) {
          await connection.invoke('JoinGroup', id);
        }
      }

      // Cargar mensajes si no están en caché
      const { mensajes } = get();
      if (!mensajes[id]) {
        let mensajesNuevos;
        if (tipo === 'grupo') {
          mensajesNuevos = await messagesApiClient.obtenerMensajesGrupo(id);
        } else {
          mensajesNuevos = await messagesApiClient.obtenerMensajes(id);
        }

        set({
          mensajes: {
            ...mensajes,
            [id]: mensajesNuevos
          }
        });
      }

      // Marcar todos como leídos (solo si es usuario por ahora, o implementar para grupos)
      if (tipo === 'usuario') {
        await messagesApiClient.marcarTodosComoLeidos(id);
      }

      set({ loading: false });
    } catch (error) {
      console.error('Error al seleccionar conversación:', error);
      set({ error: 'Error al cargar mensajes', loading: false });
    }
  },

  iniciarNuevaConversacion: async (usuarioId: string) => {
    // Simplemente seleccionar la conversación, si no existe se creará cuando se envíe el primer mensaje
    set({ conversacionActual: usuarioId });

    // Si no hay mensajes para este usuario, inicializar array vacío
    const { mensajes } = get();
    if (!mensajes[usuarioId]) {
      set({
        mensajes: {
          ...mensajes,
          [usuarioId]: []
        }
      });
    }
  },

  enviarMensaje: async (destinatarioId: string, contenido: string, grupoId?: string) => {
    try {
      const mensaje = await messagesApiClient.enviarMensaje({
        destinatarioId,
        grupoId,
        contenido,
        tipoMensaje: 'texto'
      });

      // Agregar mensaje a la lista local
      const { mensajes } = get();
      const chatKey = grupoId || destinatarioId;
      const mensajesChat = mensajes[chatKey] || [];

      set({
        mensajes: {
          ...mensajes,
          [chatKey]: [...mensajesChat, mensaje]
        }
      });

      // Recargar conversaciones
      get().cargarConversaciones();
    } catch (error) {
      console.error('Error al enviar mensaje:', error);
      set({ error: 'Error al enviar mensaje' });
      throw error;
    }
  },

  marcarComoLeido: async (mensajeId: string) => {
    try {
      await messagesApiClient.marcarComoLeido(mensajeId);
    } catch (error) {
      console.error('Error al marcar como leído:', error);
    }
  },

  notificarEscribiendo: (destinatarioId: string) => {
    const connection = getConnection();
    if (connection) {
      connection.invoke('Typing', destinatarioId);
    }
  },

  notificarDejoDeEscribir: (destinatarioId: string) => {
    const connection = getConnection();
    if (connection) {
      connection.invoke('StopTyping', destinatarioId);
    }
  }
}));
