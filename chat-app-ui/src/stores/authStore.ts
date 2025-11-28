import type { Session, User as SupabaseUser } from '@supabase/supabase-js'
import { create } from 'zustand'
import { apiClient, type User } from '../lib/api'
import { supabase } from '../lib/supabase'

interface AuthState {
  session: Session | null
  user: User | null
  loading: boolean
  signUp: (email: string, password: string, nombre: string) => Promise<void>
  signIn: (email: string, password: string) => Promise<void>
  signOut: () => Promise<void>
  initialize: () => Promise<void>
  syncUserProfile: (supabaseUser: SupabaseUser) => Promise<void>
}

export const useAuthStore = create<AuthState>((set) => ({
  session: null,
  user: null,
  loading: true,

  initialize: async () => {
    try {
      const { data: { session } } = await supabase.auth.getSession()
      
      if (session?.user) {
        // Sincronizar perfil con la API
        const user = await apiClient.syncUser({
          authId: session.user.id,
          email: session.user.email!,
          nombre: session.user.user_metadata?.nombre || session.user.email!.split('@')[0],
          avatarUrl: session.user.user_metadata?.avatar_url
        })
        
        set({ session, user, loading: false })
      } else {
        set({ session: null, user: null, loading: false })
      }

      // Escuchar cambios de autenticación
      supabase.auth.onAuthStateChange(async (_event, session) => {
        if (session?.user) {
          const user = await apiClient.syncUser({
            authId: session.user.id,
            email: session.user.email!,
            nombre: session.user.user_metadata?.nombre || session.user.email!.split('@')[0],
            avatarUrl: session.user.user_metadata?.avatar_url
          })
          set({ session, user })
        } else {
          set({ session: null, user: null })
        }
      })
    } catch (error) {
      console.error('Error initializing auth:', error)
      set({ loading: false })
    }
  },

  syncUserProfile: async (supabaseUser: SupabaseUser) => {
    const user = await apiClient.syncUser({
      authId: supabaseUser.id,
      email: supabaseUser.email!,
      nombre: supabaseUser.user_metadata?.nombre || supabaseUser.email!.split('@')[0],
      avatarUrl: supabaseUser.user_metadata?.avatar_url
    })
    set({ user })
  },

  signUp: async (email: string, password: string, nombre: string) => {
    const { data, error } = await supabase.auth.signUp({
      email,
      password,
      options: {
        data: {
          nombre
        }
      }
    })

    if (error) throw error
    
    if (data.user && data.session) {
      const user = await apiClient.syncUser({
        authId: data.user.id,
        email: data.user.email!,
        nombre,
        avatarUrl: data.user.user_metadata?.avatar_url
      })
      
      set({ session: data.session, user })
    }
  },

  signIn: async (email: string, password: string) => {
    console.log('Starting sign in for:', email)
    const { data, error } = await supabase.auth.signInWithPassword({
      email,
      password
    })
    
    console.log('Supabase sign in result:', { hasSession: !!data.session, hasUser: !!data.user, error })

    if (error) throw error
    
    if (data.user && data.session) {
      const user = await apiClient.syncUser({
        authId: data.user.id,
        email: data.user.email!,
        nombre: data.user.user_metadata?.nombre || email.split('@')[0],
        avatarUrl: data.user.user_metadata?.avatar_url
      })
      
      set({ session: data.session, user })
    }
  },

  signOut: async () => {
    const { error } = await supabase.auth.signOut()
    if (error) throw error
    set({ session: null, user: null })
  }
}))
