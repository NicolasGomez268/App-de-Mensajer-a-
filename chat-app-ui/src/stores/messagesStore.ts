import { create } from 'zustand';
import { messagesApiClient, type Conversacion, type Mensaje } from '../lib/messagesApi';
import { getConnection, startConnection, stopConnection } from '../lib/signalr';

interface MessagesState {
  conversaciones: Conversacion[];
  conversacionActual: string | null;
  mensajes: Record<string, Mensaje[]>;
  usuariosEscribiendo: Set<string>;
  loading: boolean;
  error: string | null;
  
  // Actions
  inicializarSignalR: () => Promise<void>;
  detenerSignalR: () => Promise<void>;
  cargarConversaciones: () => Promise<void>;
  seleccionarConversacion: (usuarioId: string) => Promise<void>;
  iniciarNuevaConversacion: (usuarioId: string) => Promise<void>;
  enviarMensaje: (destinatarioId: string, contenido: string) => Promise<void>;
  marcarComoLeido: (mensajeId: string) => Promise<void>;
  notificarEscribiendo: (destinatarioId: string) => void;
  notificarDejoDeEscribir: (destinatarioId: string) => void;
}

export const useMessagesStore = create<MessagesState>((set, get) => ({
  conversaciones: [],
  conversacionActual: null,
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
        // Mostrar log visible en pantalla para depuración
        window.alert(`📨 Mensaje recibido por SignalR: ${mensaje.contenido} (de ${mensaje.remitenteId} para ${mensaje.destinatarioId})`);
        const { mensajes, conversacionActual } = get();
        const usuarioId = mensaje.remitenteId;
        // Agregar mensaje a la lista
        const mensajesUsuario = mensajes[usuarioId] || [];
        set({
          mensajes: {
            ...mensajes,
            [usuarioId]: [...mensajesUsuario, {
              id: mensaje.id || crypto.randomUUID(),
              remitenteId: mensaje.remitenteId,
              destinatarioId: mensaje.destinatarioId,
              contenido: mensaje.contenido,
              tipoMensaje: mensaje.tipoMensaje || 'texto',
              fechaEnvio: mensaje.fechaEnvio || new Date().toISOString(),
              leido: false
            }]
          }
        });

        // Si es la conversación actual, marcar como leído
        if (conversacionActual === usuarioId) {
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

  seleccionarConversacion: async (usuarioId: string) => {
    set({ conversacionActual: usuarioId, loading: true, error: null });
    
    try {
      // Cargar mensajes si no están en caché
      const { mensajes } = get();
      if (!mensajes[usuarioId]) {
        const mensajesUsuario = await messagesApiClient.obtenerMensajes(usuarioId);
        set({
          mensajes: {
            ...mensajes,
            [usuarioId]: mensajesUsuario
          }
        });
      }

      // Marcar todos como leídos
      await messagesApiClient.marcarTodosComoLeidos(usuarioId);
      
      set({ loading: false });
    } catch (error) {
      console.error('Error al seleccionar conversación:', error);
      set({ error: 'Error al cargar mensajes', loading: false });
    }
  },

  iniciarNuevaConversacion: async (usuarioId: string) => {
    console.log('[messagesStore] iniciarNuevaConversacion usuarioId:', usuarioId);
    set({ conversacionActual: usuarioId });
    const { mensajes, conversaciones } = get();
    if (!mensajes[usuarioId]) {
      set({
        mensajes: {
          ...mensajes,
          [usuarioId]: []
        }
      });
    }
    // Si la conversación no existe, agregarla con datos mínimos
    if (!conversaciones.some(c => c.otroUsuarioId === usuarioId)) {
      set({
        conversaciones: [
          ...conversaciones,
          {
            id: usuarioId, // temporal, puede ser usuarioId
            otroUsuarioId: usuarioId,
            otroUsuarioNombre: 'Nuevo usuario', // puedes mejorar esto si tienes el nombre
            otroUsuarioAvatar: '',
            otroUsuarioEstado: 'offline',
            ultimoMensaje: '',
            fechaUltimoMensaje: '',
          }
        ]
      });
    }
    console.log('[messagesStore] conversacionActual:', get().conversacionActual);
  },

  enviarMensaje: async (destinatarioId: string, contenido: string) => {
    try {
      const mensaje = await messagesApiClient.enviarMensaje({
        destinatarioId,
        contenido,
        tipoMensaje: 'texto'
      });

      // Agregar mensaje a la lista local
      const { mensajes } = get();
      const mensajesUsuario = mensajes[destinatarioId] || [];
      
      set({
        mensajes: {
          ...mensajes,
          [destinatarioId]: [...mensajesUsuario, mensaje]
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
