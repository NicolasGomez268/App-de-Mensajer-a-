import { useState, type KeyboardEvent } from 'react';

interface MessageInputProps {
  onSend: (contenido: string) => Promise<void>;
  onTyping: () => void;
  onStopTyping: () => void;
  disabled?: boolean;
}

export default function MessageInput({ 
  onSend, 
  onTyping, 
  onStopTyping,
  disabled = false 
}: MessageInputProps) {
  const [mensaje, setMensaje] = useState('');
  const [enviando, setEnviando] = useState(false);
  let typingTimeout: ReturnType<typeof setTimeout>;

  const handleChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    setMensaje(e.target.value);
    
    // Notificar que está escribiendo
    onTyping();
    
    // Limpiar timeout anterior
    clearTimeout(typingTimeout);
    
    // Notificar que dejó de escribir después de 2 segundos
    typingTimeout = setTimeout(() => {
      onStopTyping();
    }, 2000);
  };

  const handleSend = async () => {
    if (!mensaje.trim() || enviando) return;

    setEnviando(true);
    try {
      await onSend(mensaje.trim());
      setMensaje('');
      onStopTyping();
    } catch (error) {
      console.error('Error al enviar mensaje:', error);
    } finally {
      setEnviando(false);
    }
  };

  const handleKeyPress = (e: KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  return (
    <div className="border-t p-4 bg-white">
      <div className="flex gap-2">
        <textarea
          value={mensaje}
          onChange={handleChange}
          onKeyPress={handleKeyPress}
          placeholder="Escribe un mensaje..."
          disabled={disabled || enviando}
          className="flex-1 resize-none border rounded-lg px-4 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-gray-100"
          rows={1}
          style={{ minHeight: '40px', maxHeight: '120px' }}
        />
        
        <button
          onClick={handleSend}
          disabled={!mensaje.trim() || disabled || enviando}
          className="px-6 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600 disabled:bg-gray-300 disabled:cursor-not-allowed transition-colors"
        >
          {enviando ? '...' : 'Enviar'}
        </button>
      </div>
    </div>
  );
}
