import { useEffect, useState } from 'react';
import ChatWindow from '../components/ChatWindow';
import ConversationList from '../components/ConversationList';
import MessageInput from '../components/MessageInput';
import UserSearchModal from '../components/UserSearchModal';
import { useAuthStore } from '../stores/authStore';
import { useMessagesStore } from '../stores/messagesStore';

export function Chat() {
  const { user, signOut } = useAuthStore();
  const {
    conversaciones,
    conversacionActual,
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
    notificarDejoDeEscribir
  } = useMessagesStore();

  const [showUserSearch, setShowUserSearch] = useState(false);

  useEffect(() => {
    // Inicializar SignalR y cargar conversaciones
    const init = async () => {
      try {
        await inicializarSignalR();
        await cargarConversaciones();
      } catch (error) {
        console.error('Error al inicializar chat:', error);
      }
    };

    init();

    // Cleanup al desmontar
    return () => {
      detenerSignalR();
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
    await seleccionarConversacion(usuarioId);
  };

  const handleEnviarMensaje = async (contenido: string) => {
    if (!conversacionActual) return;
    await enviarMensaje(conversacionActual, contenido);
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

  const handleSelectUser = async (usuarioId: string) => {
    await iniciarNuevaConversacion(usuarioId);
  };

  const conversacionActualData = conversaciones.find(
    c => c.otroUsuarioId === conversacionActual
  );

  const mensajesActuales = conversacionActual ? mensajes[conversacionActual] || [] : [];
  const usuarioEscribiendo = conversacionActual ? usuariosEscribiendo.has(conversacionActual) : false;

  return (
    <div className="h-screen flex flex-col bg-gray-100">
      {/* Header */}
      <header className="bg-white border-b border-gray-200 px-6 py-4 flex items-center justify-between">
        <div className="flex items-center space-x-4">
          <h1 className="text-xl font-bold text-gray-900">Chat App</h1>
          <span className="text-sm text-gray-500">TUP</span>
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
          conversacionActual={conversacionActual}
          onSeleccionar={handleSeleccionarConversacion}
          onNuevaConversacion={() => setShowUserSearch(true)}
        />

        {/* Chat Area */}
        <div className="flex-1 flex flex-col bg-white">
          {conversacionActual && conversacionActualData ? (
            <>
              {/* Chat Header */}
              <div className="border-b px-6 py-4 bg-white">
                <div className="flex items-center gap-3">
                  {conversacionActualData.otroUsuarioAvatar ? (
                    <img
                      src={conversacionActualData.otroUsuarioAvatar}
                      alt={conversacionActualData.otroUsuarioNombre}
                      className="w-10 h-10 rounded-full"
                    />
                  ) : (
                    <div className="w-10 h-10 rounded-full bg-gray-300 flex items-center justify-center text-white font-semibold">
                      {conversacionActualData.otroUsuarioNombre.charAt(0).toUpperCase()}
                    </div>
                  )}
                  <div>
                    <h2 className="font-semibold text-gray-900">
                      {conversacionActualData.otroUsuarioNombre}
                    </h2>
                    <p className="text-sm text-gray-500">
                      {conversacionActualData.otroUsuarioEstado || 'offline'}
                    </p>
                  </div>
                </div>
              </div>

              {/* Messages */}
              <ChatWindow
                mensajes={mensajesActuales}
                loading={loading}
                usuarioEscribiendo={usuarioEscribiendo}
                nombreOtroUsuario={conversacionActualData.otroUsuarioNombre}
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
    </div>
  );
}
