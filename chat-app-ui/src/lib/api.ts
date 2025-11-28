import { supabase } from './supabase'

const API_URL = import.meta.env.VITE_API_URL

export interface User {
  id: string
  authId: string
  email: string
  nombre: string
  avatarUrl?: string
  estado: string
  fechaCreacion: string
  ultimaConexion?: string
}

export interface SyncUserRequest {
  authId: string
  email: string
  nombre: string
  avatarUrl?: string
}

class ApiClient {
  private async getAuthToken(): Promise<string | null> {
    const { data } = await supabase.auth.getSession()
    return data.session?.access_token || null
  }

  async syncUser(userData: SyncUserRequest): Promise<User> {
    const token = await this.getAuthToken()
    
    if (!token) {
      throw new Error('No authentication token available')
    }

    console.log('Syncing user with token:', token.substring(0, 20) + '...')
    console.log('User data:', userData)
    
    const response = await fetch(`${API_URL}/api/users/sync`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify(userData)
    })

    if (!response.ok) {
      const errorText = await response.text()
      console.error('Sync failed:', response.status, errorText)
      throw new Error(`Failed to sync user: ${response.status} ${errorText}`)
    }

    return response.json()
  }

  async getMe(): Promise<User> {
    const token = await this.getAuthToken()
    
    const response = await fetch(`${API_URL}/api/users/me`, {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    })

    if (!response.ok) {
      throw new Error('Failed to get user profile')
    }

    return response.json()
  }

  async searchUsers(search?: string): Promise<User[]> {
      console.log('🔑 [searchUsers] Antes de getAuthToken');
      const token = await this.getAuthToken();
      console.log('🔑 [searchUsers] Token obtenido:', token);
      if (!token) {
        console.error('❌ [searchUsers] No authentication token available');
        throw new Error('No authentication token available');
      }

      const url = search 
        ? `${API_URL}/api/users?search=${encodeURIComponent(search)}`
        : `${API_URL}/api/users`;
      console.log('🌐 Calling:', url);
      console.log('🔑 Token present:', !!token);

      try {
        const response = await fetch(url, {
          headers: {
            'Authorization': `Bearer ${token}`
          }
        });
        console.log('📡 Response status:', response.status);
        if (!response.ok) {
          const errorText = await response.text();
          console.error('❌ API Error:', errorText);
          throw new Error(`Failed to search users: ${response.status} ${errorText}`);
        }
        const result = await response.json();
        console.log('📋 API Result:', result);
        return result;
      } catch (err) {
        console.error('❌ [searchUsers] Error en fetch:', err);
        throw err;
      }
  }

  async updateStatus(estado: string): Promise<void> {
    const token = await this.getAuthToken()
    
    const response = await fetch(`${API_URL}/api/users/status`, {
      method: 'PATCH',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify(estado)
    })

    if (!response.ok) {
      throw new Error('Failed to update status')
    }
  }
}

export const apiClient = new ApiClient()
