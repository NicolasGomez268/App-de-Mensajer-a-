import { useEffect, useState } from 'react';
import ChatWindow from '../components/ChatWindow';
import ConversationList from '../components/ConversationList';
import MessageInput from '../components/MessageInput';
import UserSearchModal from '../components/UserSearchModal';
import { useAuthStore } from '../stores/authStore';
import { useMessagesStore } from '../stores/messagesStore';
import { useGroupsStore } from '../stores/groupsStore';
import CreateGroupModal from '../components/CreateGroupModal';

export function Chat() {
  const { user, signOut } = useAuthStore();
  const {
    conversaciones,
    conversacionActual,
    tipoConversacionActual,
    mensajes,
    usuariosEscribiendo,
    loading,
    inicializarSignalR,
    detenerSignalR,
    cargarConversaciones,
    seleccionarConversacion,
    iniciarNuevaConversacion,
    enviarMensaje,
    notificarEscribiendo,
    notificarDejoDeEscribir,
    unirseAGrupos
  } = useMessagesStore();

  const { grupos, cargarGrupos, inicializarEventos } = useGroupsStore();

  const [showUserSearch, setShowUserSearch] = useState(false);
  const [showCreateGroup, setShowCreateGroup] = useState(false);
  const [connectionStatus, setConnectionStatus] = useState<'connected' | 'disconnected' | 'connecting'>('disconnected');

  // Efecto para cerrar el modal y loggear cuando cambia conversacionActual
  useEffect(() => {
    console.log('[Chat] conversacionActual cambió:', conversacionActual);
    if (conversacionActual && showUserSearch) {
      setShowUserSearch(false);
    }
  }, [conversacionActual]);

  // Inicializar SignalR y cargar conversaciones
  useEffect(() => {
    let cleanupGroupsEvents: () => void;

    const init = async () => {
      try {
        setConnectionStatus('connecting');
        await inicializarSignalR();
        setConnectionStatus('connected');

        cleanupGroupsEvents = inicializarEventos();
        await Promise.all([
          cargarConversaciones(),
          cargarGrupos()
        ]);

        // Unirse a los grupos cargados para recibir notificaciones
        const gruposCargados = useGroupsStore.getState().grupos;
        if (gruposCargados.length > 0) {
          await unirseAGrupos(gruposCargados.map(g => g.id));
        }
      } catch (error) {
        console.error('Error al inicializar chat:', error);
        setConnectionStatus('disconnected');
      }
    };

    init();

    // Cleanup al desmontar
    return () => {
      detenerSignalR();
      if (cleanupGroupsEvents) cleanupGroupsEvents();
    };
  }, []);

  const handleSignOut = async () => {
    try {
      await detenerSignalR();
      await signOut();
    } catch (error) {
      console.error('Error signing out:', error);
    }
  };

  const handleSeleccionarConversacion = async (usuarioId: string) => {
    await seleccionarConversacion(usuarioId, 'usuario');
  };

  const handleEnviarMensaje = async (contenido: string) => {
    if (!conversacionActual) return;

    if (tipoConversacionActual === 'grupo') {
      await enviarMensaje(conversacionActual, contenido, conversacionActual);
    } else {
      await enviarMensaje(conversacionActual, contenido);
    }
  };

  const handleSeleccionarGrupo = async (grupoId: string) => {
    console.log('Grupo seleccionado:', grupoId);
    await seleccionarConversacion(grupoId, 'grupo');
  };

  const handleTyping = () => {
    if (conversacionActual) {
      notificarEscribiendo(conversacionActual);
    }
  };

  const handleStopTyping = () => {
    if (conversacionActual) {
      notificarDejoDeEscribir(conversacionActual);
    }
  };

  const handleSelectUser = async (usuario: any) => {
    console.log('[Chat] handleSelectUser usuario:', usuario);
    await iniciarNuevaConversacion({
      id: usuario.id,
      nombre: usuario.nombre,
      avatarUrl: usuario.avatarUrl,
      estado: usuario.estado
    });
    console.log('[Chat] Conversacion actual después de seleccionar:', conversacionActual);
  };

  const conversacionActualData = tipoConversacionActual === 'grupo'
    ? grupos.find(g => g.id === conversacionActual)
    : conversaciones.find(c => c.otroUsuarioId === conversacionActual);

  const mensajesActuales = conversacionActual ? mensajes[conversacionActual] || [] : [];
  const usuarioEscribiendo = conversacionActual ? usuariosEscribiendo.has(conversacionActual) : false;

  const nombreChat = tipoConversacionActual === 'grupo'
    ? (conversacionActualData as any)?.nombre
    : (conversacionActualData as any)?.otroUsuarioNombre;

  const avatarChat = tipoConversacionActual === 'grupo'
    ? null // TODO: Group avatar
    : (conversacionActualData as any)?.otroUsuarioAvatar;

  const estadoChat = tipoConversacionActual === 'grupo'
    ? (conversacionActualData as any)?.miembros?.map((m: any) => m.nombreUsuario).join(', ') || 'Sin miembros'
    : (conversacionActualData as any)?.otroUsuarioEstado || 'offline';

  return (
    <div className="h-screen flex flex-col bg-gray-100">
      {/* Header */}
      <header className="bg-white border-b border-gray-200 px-6 py-4 flex items-center justify-between">
        <div className="flex items-center space-x-4">
          <h1 className="text-xl font-bold text-gray-900">Chat App</h1>
          <span className="text-sm text-gray-500">TUP</span>
          <div className="flex items-center" title={`SignalR: ${connectionStatus}`}>
            <span className={`w-3 h-3 rounded-full ${connectionStatus === 'connected' ? 'bg-green-500' :
              connectionStatus === 'connecting' ? 'bg-yellow-500' : 'bg-red-500'
              }`}></span>
          </div>
        </div>
        <div className="flex items-center space-x-4">
          <div className="text-right">
            <p className="text-sm font-medium text-gray-900">{user?.nombre}</p>
            <p className="text-xs text-gray-500">{user?.email}</p>
          </div>
          <button
            onClick={handleSignOut}
            className="px-4 py-2 text-sm font-medium text-white bg-red-600 rounded-md hover:bg-red-700"
          >
            Cerrar Sesión
          </button>
        </div>
      </header>

      {/* Main Content */}
      <div className="flex-1 flex overflow-hidden">
        {/* Sidebar - Lista de conversaciones */}
        <ConversationList
          conversaciones={conversaciones}
          grupos={grupos}
          conversacionActual={conversacionActual}
          onSeleccionar={handleSeleccionarConversacion}
          onSeleccionarGrupo={handleSeleccionarGrupo}
          onNuevaConversacion={() => setShowUserSearch(true)}
          onNuevoGrupo={() => setShowCreateGroup(true)}
        />

        {/* Chat Area */}
        <div className="flex-1 flex flex-col bg-white">
          {conversacionActual && conversacionActualData ? (
            <>
              {/* Chat Header */}
              <div className="border-b px-6 py-4 bg-white">
                <div className="flex items-center gap-3">
                  {avatarChat ? (
                    <img
                      src={avatarChat}
                      alt={nombreChat}
                      className="w-10 h-10 rounded-full"
                    />
                  ) : (
                    <div className="w-10 h-10 rounded-full bg-gray-300 flex items-center justify-center text-white font-semibold">
                      {nombreChat?.charAt(0).toUpperCase()}
                    </div>
                  )}
                  <div>
                    <h2 className="font-semibold text-gray-900">
                      {nombreChat}
                    </h2>
                    <p className="text-sm text-gray-500">
                      {estadoChat}
                    </p>
                  </div>
                </div>
              </div>

              {/* Messages */}
              <ChatWindow
                mensajes={mensajesActuales}
                loading={loading}
                usuarioEscribiendo={usuarioEscribiendo}
                nombreOtroUsuario={nombreChat}
                esGrupo={tipoConversacionActual === 'grupo'}
              />

              {/* Message Input */}
              <MessageInput
                onSend={handleEnviarMensaje}
                onTyping={handleTyping}
                onStopTyping={handleStopTyping}
              />
            </>
          ) : (
            <div className="flex-1 flex items-center justify-center bg-gray-50">
              <div className="text-center text-gray-500">
                <svg
                  className="w-24 h-24 mx-auto mb-4 text-gray-300"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M8 10h.01M12 10h.01M16 10h.01M9 16H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-5l-5 5v-5z"
                  />
                </svg>
                <h3 className="text-lg font-medium mb-2">¡Bienvenido/a, {user?.nombre}!</h3>
                <p>Selecciona una conversación para comenzar</p>
                <p className="text-sm mt-2">o busca un usuario para iniciar un nuevo chat</p>
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Modal de búsqueda de usuarios */}
      <UserSearchModal
        isOpen={showUserSearch}
        onClose={() => setShowUserSearch(false)}
        onSelectUser={handleSelectUser}
      />

      {/* Modal de crear grupo */}
      <CreateGroupModal
        isOpen={showCreateGroup}
        onClose={() => setShowCreateGroup(false)}
      />
    </div>
  );
}
