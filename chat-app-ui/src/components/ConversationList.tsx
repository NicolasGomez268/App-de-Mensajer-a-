import type { Conversacion } from '../lib/messagesApi';
import type { Grupo } from '../lib/groupsApi';

interface ConversationListProps {
  conversaciones: Conversacion[];
  grupos: Grupo[];
  conversacionActual: string | null;
  onSeleccionar: (usuarioId: string) => void;
  onSeleccionarGrupo: (grupoId: string) => void;
  onNuevaConversacion: () => void;
  onNuevoGrupo: () => void;
}

export default function ConversationList({
  conversaciones,
  grupos,
  conversacionActual,
  onSeleccionar,
  onSeleccionarGrupo,
  onNuevaConversacion,
  onNuevoGrupo
}: ConversationListProps) {
  const formatTime = (fecha: string) => {
    const date = new Date(fecha);
    const now = new Date();
    const diff = now.getTime() - date.getTime();
    const hours = Math.floor(diff / (1000 * 60 * 60));

    if (hours < 24) {
      return date.toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' });
    } else if (hours < 48) {
      return 'Ayer';
    } else {
      return date.toLocaleDateString('es-AR', { day: '2-digit', month: '2-digit' });
    }
  };

  const getEstadoColor = (estado?: string) => {
    switch (estado?.toLowerCase()) {
      case 'online':
        return 'bg-green-500';
      case 'ausente':
        return 'bg-yellow-500';
      default:
        return 'bg-gray-400';
    }
  };

  return (
    <div className="w-80 border-r bg-gray-50 overflow-y-auto">
      <div className="p-4 border-b bg-white">
        <div className="flex items-center justify-between mb-2">
          <h2 className="text-xl font-semibold">Mensajes</h2>
          <button
            onClick={onNuevaConversacion}
            className="p-2 text-blue-500 hover:bg-blue-50 rounded-full"
            title="Nueva conversación"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
            </svg>
          </button>
        </div>
        <div className="flex items-center justify-between">
          <h2 className="text-sm font-semibold text-gray-500 uppercase">Grupos</h2>
          <button
            onClick={onNuevoGrupo}
            className="p-1 text-blue-500 hover:bg-blue-50 rounded-full"
            title="Nuevo grupo"
          >
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
            </svg>
          </button>
        </div>
      </div>

      <div className="divide-y">
        {/* Lista de Grupos */}
        {grupos.map((grupo) => (
          <div
            key={grupo.id}
            onClick={() => onSeleccionarGrupo(grupo.id)}
            className={`p-4 cursor-pointer hover:bg-gray-100 transition-colors ${conversacionActual === grupo.id ? 'bg-blue-50 border-l-4 border-blue-500' : ''
              }`}
          >
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-lg bg-indigo-100 flex items-center justify-center text-indigo-600 font-bold">
                #
              </div>
              <div>
                <h3 className="font-semibold text-gray-900">{grupo.nombre}</h3>
                <p className="text-xs text-gray-500">Grupo</p>
              </div>
            </div>
          </div>
        ))}

        {/* Separador */}
        {grupos.length > 0 && <div className="bg-gray-100 px-4 py-2 text-xs font-semibold text-gray-500 uppercase">Chats Directos</div>}

        {conversaciones.length === 0 && grupos.length === 0 ? (
          <div className="p-8 text-center text-gray-500">
            <p>No hay conversaciones</p>
            <p className="text-sm mt-2">Busca un usuario para comenzar a chatear</p>
          </div>
        ) : (
          conversaciones.map((conv) => (
            <div
              key={conv.id}
              onClick={() => onSeleccionar(conv.otroUsuarioId)}
              className={`p-4 cursor-pointer hover:bg-gray-100 transition-colors ${conversacionActual === conv.otroUsuarioId ? 'bg-blue-50 border-l-4 border-blue-500' : ''
                }`}
            >
              <div className="flex items-start gap-3">
                {/* Avatar */}
                <div className="relative flex-shrink-0">
                  {conv.otroUsuarioAvatar ? (
                    <img
                      src={conv.otroUsuarioAvatar}
                      alt={conv.otroUsuarioNombre}
                      className="w-12 h-12 rounded-full"
                    />
                  ) : (
                    <div className="w-12 h-12 rounded-full bg-gray-300 flex items-center justify-center text-white font-semibold">
                      {conv.otroUsuarioNombre.charAt(0).toUpperCase()}
                    </div>
                  )}

                  {/* Indicador de estado */}
                  <div
                    className={`absolute bottom-0 right-0 w-3 h-3 rounded-full border-2 border-white ${getEstadoColor(
                      conv.otroUsuarioEstado
                    )}`}
                  />
                </div>

                {/* Información */}
                <div className="flex-1 min-w-0">
                  <div className="flex items-baseline justify-between gap-2">
                    <h3 className="font-semibold text-gray-900 truncate">
                      {conv.otroUsuarioNombre}
                    </h3>
                    {conv.ultimoMensaje && (
                      <span className="text-xs text-gray-500 flex-shrink-0">
                        {formatTime(conv.ultimoMensaje.fechaEnvio)}
                      </span>
                    )}
                  </div>

                  {conv.ultimoMensaje && (
                    <p className="text-sm text-gray-600 truncate mt-1">
                      {conv.ultimoMensaje.contenido}
                    </p>
                  )}

                  {conv.mensajesNoLeidos > 0 && (
                    <div className="mt-1">
                      <span className="inline-flex items-center justify-center px-2 py-1 text-xs font-bold text-white bg-blue-500 rounded-full">
                        {conv.mensajesNoLeidos}
                      </span>
                    </div>
                  )}
                </div>
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  );
}
