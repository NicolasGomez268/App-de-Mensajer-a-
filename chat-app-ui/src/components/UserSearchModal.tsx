import { useState } from 'react';
import { apiClient, type User } from '../lib/api';

interface UserSearchModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSelectUser: (usuario: any) => void;
}

export default function UserSearchModal({ isOpen, onClose, onSelectUser }: UserSearchModalProps) {
  const [busqueda, setBusqueda] = useState('');
  const [usuarios, setUsuarios] = useState<User[]>([]);
  const [buscando, setBuscando] = useState(false);

  const buscarUsuarios = async () => {
    if (!busqueda.trim()) return;

    setBuscando(true);
    try {
      console.log('🔍 Buscando usuarios:', busqueda);
      const resultados = await apiClient.searchUsers(busqueda);
      console.log('✅ Usuarios encontrados:', resultados);
      setUsuarios(resultados);
    } catch (error) {
      console.error('❌ Error al buscar usuarios:', error);
      alert('Error al buscar usuarios. Verifica la consola para más detalles.');
    } finally {
      setBuscando(false);
    }
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    buscarUsuarios();
  };

  const handleSelectUser = (usuario: User) => {
    onSelectUser(usuario);
    onClose();
    setBusqueda('');
    setUsuarios([]);
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg p-6 w-96 max-h-96">
        <div className="flex justify-between items-center mb-4">
          <h2 className="text-lg font-semibold">Buscar Usuarios</h2>
          <button
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600"
          >
            ✕
          </button>
        </div>

        <form onSubmit={handleSubmit} className="mb-4">
          <div className="flex gap-2">
            <input
              type="text"
              value={busqueda}
              onChange={(e) => setBusqueda(e.target.value)}
              placeholder="Buscar por nombre o email..."
              className="flex-1 px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
            <button
              type="submit"
              disabled={buscando || !busqueda.trim()}
              className="px-4 py-2 bg-blue-500 text-white rounded-md hover:bg-blue-600 disabled:bg-gray-300"
            >
              {buscando ? '...' : 'Buscar'}
            </button>
          </div>
        </form>

        <div className="max-h-48 overflow-y-auto">
          {usuarios.length === 0 && !buscando ? (
            <p className="text-gray-500 text-center py-4">
              Busca usuarios para iniciar una conversación
            </p>
          ) : (
            usuarios.map((usuario) => (
              <div
                key={usuario.id}
                onClick={() => handleSelectUser(usuario)}
                className="flex items-center gap-3 p-3 hover:bg-gray-50 cursor-pointer rounded-md"
              >
                {usuario.avatarUrl ? (
                  <img
                    src={usuario.avatarUrl}
                    alt={usuario.nombre}
                    className="w-10 h-10 rounded-full"
                  />
                ) : (
                  <div className="w-10 h-10 rounded-full bg-gray-300 flex items-center justify-center text-white font-semibold">
                    {usuario.nombre?.charAt(0).toUpperCase() || '?'}
                  </div>
                )}
                <div className="flex-1">
                  <p className="font-medium">{usuario.nombre}</p>
                  <p className="text-sm text-gray-500">{usuario.email}</p>
                </div>
                <div className="text-xs text-gray-400">
                  {usuario.estado || 'offline'}
                </div>
              </div>
            ))
          )}
        </div>
      </div>
    </div>
  );
}