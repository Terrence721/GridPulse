import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react'
import { User } from 'oidc-client-ts'
import { oidcConfig } from '../auth/oidcConfig.ts'

function getAccessToken(): string | undefined {
  const key = `oidc.user:${oidcConfig.authority}:${oidcConfig.client_id}`
  const stored = sessionStorage.getItem(key)
  return stored ? User.fromStorageString(stored).access_token : undefined
}

interface MeResponse {
  name: string | null
  claims: { type: string; value: string }[]
}

export const gatewayApi = createApi({
  reducerPath: 'gatewayApi',
  baseQuery: fetchBaseQuery({
    baseUrl: import.meta.env.VITE_GATEWAY_URL,
    prepareHeaders: (headers) => {
      const token = getAccessToken()
      if (token) {
        headers.set('Authorization', `Bearer ${token}`)
      }
      return headers
    },
  }),
  endpoints: (builder) => ({
    getMe: builder.query<MeResponse, void>({
      query: () => '/api/me',
    }),
  }),
})

export const { useGetMeQuery } = gatewayApi
