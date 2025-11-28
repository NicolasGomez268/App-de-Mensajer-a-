import { create } from 'zustand';
import { groupsApiClient, type Grupo } from '../lib/groupsApi';

interface GroupsState {
    grupos: Grupo[];
    loading: boolean;
    error: string | null;

    cargarGrupos: () => Promise<void>;
    crearGrupo: (nombre: string, miembrosIds: string[]) => Promise<void>;
    eliminarGrupo: (id: string) => Promise<void>;
    inicializarEventos: () => () => void;
}

export const useGroupsStore = create<GroupsState>((set, get) => ({
    grupos: [],
    loading: false,
    error: null,

    cargarGrupos: async () => {
        set({ loading: true, error: null });
        try {
            const grupos = await groupsApiClient.misGrupos();
            set({ grupos, loading: false });
        } catch (error) {
            console.error('Error loading groups:', error);
            set({ error: 'Error al cargar grupos', loading: false });
        }
    },

    crearGrupo: async (nombre: string, miembrosIds: string[]) => {
        set({ loading: true, error: null });
        try {
            await groupsApiClient.crearGrupo({ nombre, miembrosIds });
            // Recargar la lista después de crear
            await get().cargarGrupos();
            set({ loading: false });
        } catch (error) {
            console.error('Error creating group:', error);
            set({ error: 'Error al crear el grupo', loading: false });
            throw error;
        }
    },

    eliminarGrupo: async (id: string) => {
        try {
            await groupsApiClient.eliminarGrupo(id);
            set((state) => ({
                grupos: state.grupos.filter(g => g.id !== id)
            }));
        } catch (error) {
            console.error('Error deleting group:', error);
            throw error;
        }
    },

    inicializarEventos: () => {
        const handler = () => {
            console.log('🔄 Recargando grupos por evento group-added');
            get().cargarGrupos();
        };
        window.addEventListener('group-added', handler);
        return () => window.removeEventListener('group-added', handler);
    }
}));
