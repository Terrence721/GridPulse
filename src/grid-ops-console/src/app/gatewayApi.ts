import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react'
import { User } from 'oidc-client-ts'
import { oidcConfig } from '../auth/oidcConfig.ts'

function getAccessToken(): string | undefined {
  const key = `oidc.user:${oidcConfig.authority}:${oidcConfig.client_id}`
  const stored = sessionStorage.getItem(key)
  return stored ? User.fromStorageString(stored).access_token : undefined
}

export interface WorkOrder {
  id: string
  hazardType: string
  meterId: string | null
  accountId: string | null
  streetName: string | null
  streetNumber: number | null
  description: string
  assignedCrew: string | null
  status: string
  outageId: string | null
  createdAt: string
  updatedAt: string
}

export interface Outage {
  id: string
  streetName: string
  streetNumberRangeStart: number
  streetNumberRangeEnd: number
  detectedAt: string
  status: string
}

export interface WorkOrderDetail {
  workOrder: WorkOrder
  outage: Outage | null
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
    getWorkOrders: builder.query<WorkOrder[], void>({
      query: () => '/api/work-orders',
    }),
    getOutages: builder.query<Outage[], void>({
      query: () => '/api/outages',
    }),
    getWorkOrder: builder.query<WorkOrderDetail, string>({
      query: (id) => `/api/work-orders/${id}`,
    }),
  }),
})

export const { useGetWorkOrdersQuery, useGetOutagesQuery, useGetWorkOrderQuery } = gatewayApi
