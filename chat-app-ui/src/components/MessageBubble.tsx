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
    <div className={`flex ${esMio ? 'justify-end' : 'justify-start'} mb-2 group`}>
      <div className={`flex flex-col max-w-[75%] ${esMio ? 'items-end' : 'items-start'}`}>
        {!esMio && mostrarNombre && mensaje.remitenteNombre && (
          <span className="text-xs text-gray-500 ml-2 mb-1">
            {mensaje.remitenteNombre}
          </span>
        )}

        <div
          className={`relative px-4 py-2 shadow-sm text-sm ${esMio
            ? 'bg-blue-600 text-white rounded-2xl rounded-tr-none'
            : 'bg-gray-100 text-gray-800 rounded-2xl rounded-tl-none'
            }`}
        >
          <p className="break-words leading-relaxed">{mensaje.contenido}</p>

          <div className={`flex items-center justify-end gap-1 mt-1 select-none ${esMio ? 'text-blue-100' : 'text-gray-400'
            }`}>
            <span className="text-[10px]">{formatTime(mensaje.fechaEnvio)}</span>
            {esMio && (
              <span className={`text-[10px] ${mensaje.leido ? 'text-cyan-300' : 'text-blue-300'}`} title={mensaje.leido ? "Leído" : "Entregado"}>
                <svg className="w-4 h-4" viewBox="0 0 16 15" width="16" height="15" fill="currentColor">
                  <path d="M15.01 3.316l-.478-.372a.365.365 0 0 0-.51.063L8.666 9.879a.32.32 0 0 1-.484.033l-.358-.325a.319.319 0 0 0-.484.032l-.378.483a.418.418 0 0 0 .036.541l1.32 1.266c.143.14.361.125.484-.033l6.272-7.46a.41.41 0 0 0-.066-.54M11.027 3.316l-.479-.372a.365.365 0 0 0-.509.063L4.683 9.879a.32.32 0 0 1-.484.033l-2.45-2.224a.418.418 0 0 0-.54.036l-.378.483a.418.418 0 0 0 .036.541l3.32 3.267c.143.14.361.125.484-.033l6.272-7.46a.41.41 0 0 0-.066-.54"></path>
                </svg>
              </span>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
