import { useState, useEffect } from 'react';
import { useGroupsStore } from '../stores/groupsStore';
import { apiClient, type User } from '../lib/api';

interface CreateGroupModalProps {
    isOpen: boolean;
    onClose: () => void;
}

export default function CreateGroupModal({ isOpen, onClose }: CreateGroupModalProps) {
    const [nombre, setNombre] = useState('');
    const [busqueda, setBusqueda] = useState('');
    const [usuarios, setUsuarios] = useState<User[]>([]);
    const [miembrosSeleccionados, setMiembrosSeleccionados] = useState<User[]>([]);
    const [loading, setLoading] = useState(false);
    const { crearGrupo } = useGroupsStore();

    useEffect(() => {
        if (isOpen) {
            setNombre('');
            setBusqueda('');
            setMiembrosSeleccionados([]);
            setUsuarios([]);
        }
    }, [isOpen]);

    useEffect(() => {
        const buscarUsuarios = async () => {
            if (busqueda.length < 2) {
                setUsuarios([]);
                return;
            }

            try {
                const resultados = await apiClient.searchUsers(busqueda);
                setUsuarios(resultados.filter(u => !miembrosSeleccionados.find(m => m.id === u.id)));
            } catch (error) {
                console.error('Error buscando usuarios:', error);
            }
        };

        const timeoutId = setTimeout(buscarUsuarios, 300);
        return () => clearTimeout(timeoutId);
    }, [busqueda, miembrosSeleccionados]);

    const handleCrear = async () => {
        if (!nombre.trim() || miembrosSeleccionados.length === 0) return;

        setLoading(true);
        try {
            await crearGrupo(nombre, miembrosSeleccionados.map(u => u.id));
            onClose();
        } catch (error) {
            console.error('Error al crear grupo:', error);
            alert('Error al crear el grupo');
        } finally {
            setLoading(false);
        }
    };

    const toggleMiembro = (usuario: User) => {
        if (miembrosSeleccionados.find(m => m.id === usuario.id)) {
            setMiembrosSeleccionados(prev => prev.filter(m => m.id !== usuario.id));
        } else {
            setMiembrosSeleccionados(prev => [...prev, usuario]);
            setBusqueda(''); // Limpiar búsqueda al seleccionar
        }
    };

    if (!isOpen) return null;

    return (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
            <div className="bg-white rounded-lg p-6 w-full max-w-md">
                <h2 className="text-xl font-bold mb-4">Crear Nuevo Grupo</h2>

                <div className="space-y-4">
                    <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1">
                            Nombre del Grupo
                        </label>
                        <input
                            type="text"
                            value={nombre}
                            onChange={(e) => setNombre(e.target.value)}
                            className="w-full border rounded-md px-3 py-2"
                            placeholder="Ej: Proyecto Final"
                        />
                    </div>

                    <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1">
                            Agregar Miembros
                        </label>
                        <div className="flex flex-wrap gap-2 mb-2">
                            {miembrosSeleccionados.map(user => (
                                <span key={user.id} className="bg-blue-100 text-blue-800 text-xs px-2 py-1 rounded-full flex items-center">
                                    {user.nombre}
                                    <button
                                        onClick={() => toggleMiembro(user)}
                                        className="ml-1 text-blue-600 hover:text-blue-800"
                                    >
                                        ×
                                    </button>
                                </span>
                            ))}
                        </div>
                        <input
                            type="text"
                            value={busqueda}
                            onChange={(e) => setBusqueda(e.target.value)}
                            className="w-full border rounded-md px-3 py-2"
                            placeholder="Buscar usuarios..."
                        />

                        {usuarios.length > 0 && (
                            <div className="mt-2 border rounded-md max-h-40 overflow-y-auto">
                                {usuarios.map(user => (
                                    <div
                                        key={user.id}
                                        onClick={() => toggleMiembro(user)}
                                        className="p-2 hover:bg-gray-100 cursor-pointer flex items-center gap-2"
                                    >
                                        <div className="w-8 h-8 bg-gray-200 rounded-full flex items-center justify-center text-sm font-bold">
                                            {user.nombre.charAt(0).toUpperCase()}
                                        </div>
                                        <div>
                                            <p className="text-sm font-medium">{user.nombre}</p>
                                            <p className="text-xs text-gray-500">{user.email}</p>
                                        </div>
                                    </div>
                                ))}
                            </div>
                        )}
                    </div>
                </div>

                <div className="mt-6 flex justify-end gap-3">
                    <button
                        onClick={onClose}
                        className="px-4 py-2 text-gray-600 hover:bg-gray-100 rounded-md"
                    >
                        Cancelar
                    </button>
                    <button
                        onClick={handleCrear}
                        disabled={!nombre.trim() || miembrosSeleccionados.length === 0 || loading}
                        className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50"
                    >
                        {loading ? 'Creando...' : 'Crear Grupo'}
                    </button>
                </div>
            </div>
        </div>
    );
}
