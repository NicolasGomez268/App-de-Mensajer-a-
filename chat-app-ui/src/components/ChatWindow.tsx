import { useEffect, useRef } from 'react';
import type { Mensaje } from '../lib/messagesApi';
import { useAuthStore } from '../stores/authStore';
import MessageBubble from './MessageBubble';

interface ChatWindowProps {
  mensajes: Mensaje[];
  loading: boolean;
  usuarioEscribiendo: boolean;
  nombreOtroUsuario?: string;
  esGrupo?: boolean;
}

export default function ChatWindow({
  mensajes,
  loading,
  usuarioEscribiendo,
  nombreOtroUsuario,
  esGrupo = false
}: ChatWindowProps) {
  const { user } = useAuthStore();
  const messagesEndRef = useRef<HTMLDivElement>(null);

  // Auto-scroll al final cuando hay mensajes nuevos
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [mensajes]);

  if (loading) {
    return (
      <div className="flex-1 flex items-center justify-center bg-gray-50">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500 mx-auto mb-4"></div>
          <p className="text-gray-600">Cargando mensajes...</p>
        </div>
      </div>
    );
  }

  if (mensajes.length === 0) {
    return (
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
              d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z"
            />
          </svg>
          <p className="text-lg font-medium">No hay mensajes aún</p>
          <p className="text-sm mt-2">Envía un mensaje para comenzar la conversación</p>
        </div>
      </div>
    );
  }

  return (
    <div className="flex-1 overflow-y-auto bg-gray-50 p-4">
      {mensajes.map((mensaje) => {
        // El remitenteId en el mensaje es el AuthId (Supabase ID)
        // El user.id en el store es el ID interno de la BD
        // El user.authId en el store es el AuthId (Supabase ID)
        const esMio = mensaje.remitenteId === user?.authId || mensaje.remitenteId === user?.id;

        return (
          <MessageBubble
            key={mensaje.id}
            mensaje={mensaje}
            esMio={esMio}
            mostrarNombre={esGrupo && !esMio}
          />
        );
      })}

      {usuarioEscribiendo && (
        <div className="flex items-center gap-2 text-gray-500 text-sm mb-4">
          <div className="flex gap-1">
            <div className="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style={{ animationDelay: '0ms' }}></div>
            <div className="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style={{ animationDelay: '150ms' }}></div>
            <div className="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style={{ animationDelay: '300ms' }}></div>
          </div>
          <span>{nombreOtroUsuario} está escribiendo...</span>
        </div>
      )}

      <div ref={messagesEndRef} />
    </div>
  );
}
