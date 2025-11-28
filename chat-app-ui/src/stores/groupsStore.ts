import { create } from 'zustand';
import { groupsApiClient, type Grupo } from '../lib/groupsApi';

interface GroupsState {
    grupos: Grupo[];
    loading: boolean;
    error: string | null;

    cargarGrupos: () => Promise<void>;
    crearGrupo: (nombre: string, miembrosIds: string[]) => Promise<void>;
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
    }
}));
