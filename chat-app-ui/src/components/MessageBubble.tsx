import type { Mensaje } from '../lib/messagesApi';

interface MessageBubbleProps {
  mensaje: Mensaje;
  esMio: boolean;
  mostrarNombre?: boolean;
}

export default function MessageBubble({ mensaje, esMio, mostrarNombre = false }: MessageBubbleProps) {
  const formatTime = (fecha: string) => {
    const date = new Date(fecha);
    return date.toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' });
  };

  return (
    <div className={`flex ${esMio ? 'justify-end' : 'justify-start'} mb-4`}>
      <div className={`max-w-[70%] ${esMio ? 'order-2' : 'order-1'}`}>
        {!esMio && mostrarNombre && mensaje.remitenteNombre && (
          <div className="text-sm text-gray-600 mb-1 px-2">
            {mensaje.remitenteNombre}
          </div>
        )}

        <div
          className={`rounded-lg px-4 py-2 ${esMio
              ? 'bg-blue-500 text-white rounded-br-none'
              : 'bg-gray-200 text-gray-900 rounded-bl-none'
            }`}
        >
          <p className="break-words">{mensaje.contenido}</p>

          <div className={`text-xs mt-1 flex items-center gap-1 ${esMio ? 'text-blue-100' : 'text-gray-500'
            }`}>
            <span>{formatTime(mensaje.fechaEnvio)}</span>
            {esMio && (
              <span>
                {mensaje.leido ? '✓✓' : '✓'}
              </span>
            )}
          </div>
        </div>
      </div>

      {!esMio && mensaje.remitenteAvatar && (
        <img
          src={mensaje.remitenteAvatar}
          alt={mensaje.remitenteNombre}
          className="w-8 h-8 rounded-full order-0 mr-2"
        />
      )}
    </div>
  );
}
