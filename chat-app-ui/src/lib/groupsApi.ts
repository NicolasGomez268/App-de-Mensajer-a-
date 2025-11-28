import { supabase } from './supabase';

const GRUPOS_API_URL = import.meta.env.VITE_GRUPOS_API_URL || 'http://localhost:5022';

export interface Grupo {
    id: string;
    nombre: string;
    creadoPor: string;
    fechaCreacion: string;
    miembros?: MiembroGrupo[];
}

export interface CrearGrupoRequest {
    nombre: string;
    miembrosIds: string[];
}

export interface MiembroGrupo {
    id: string;
    grupoId: string;
    usuarioId: string;
    fechaIngreso: string;
    nombreUsuario: string;
    emailUsuario: string;
}

class GroupsApiClient {
    private async getAuthToken(): Promise<string | null> {
        const { data } = await supabase.auth.getSession();
        return data.session?.access_token || null;
    }

    async crearGrupo(request: CrearGrupoRequest): Promise<Grupo> {
        const token = await this.getAuthToken();
        if (!token) throw new Error('No authentication token available');

        const response = await fetch(`${GRUPOS_API_URL}/api/grupos`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify(request)
        });

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(`Failed to create group: ${response.status} ${errorText}`);
        }

        return response.json();
    }

    async misGrupos(): Promise<Grupo[]> {
        const token = await this.getAuthToken();
        if (!token) throw new Error('No authentication token available');

        const response = await fetch(`${GRUPOS_API_URL}/api/grupos/mis-grupos`, {
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });

        if (!response.ok) {
            throw new Error('Failed to get groups');
        }

        return response.json();
    }

    async obtenerMiembros(grupoId: string): Promise<MiembroGrupo[]> {
        const token = await this.getAuthToken();
        if (!token) throw new Error('No authentication token available');

        const response = await fetch(`${GRUPOS_API_URL}/api/grupos/${grupoId}/miembros`, {
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });

        if (!response.ok) {
            throw new Error('Failed to get members');
        }

        return response.json();
    }

    async agregarMiembro(grupoId: string, userId: string): Promise<void> {
        const token = await this.getAuthToken();
        if (!token) throw new Error('No authentication token available');

        const response = await fetch(`${GRUPOS_API_URL}/api/grupos/${grupoId}/miembros`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify({ userId })
        });

        if (!response.ok) {
            throw new Error('Failed to add member');
        }
    }
}

export const groupsApiClient = new GroupsApiClient();
