import axios from 'axios'
import type { ScanHistoryItem, ScanReport, ScanRequest } from '../types/scan'

export const scanApi = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:8082'
})

let accessToken: string | null = null

async function ensureDevToken(): Promise<void> {
  if (accessToken) {
    return
  }

  const { data } = await scanApi.post<{ accessToken: string }>('/api/auth/dev-token')
  accessToken = data.accessToken
}

scanApi.interceptors.request.use(async (config) => {
  if (config.url?.startsWith('/api/health') || config.url?.startsWith('/api/auth/')) {
    return config
  }

  await ensureDevToken()
  config.headers.Authorization = `Bearer ${accessToken}`
  return config
})

export async function scanFromUrl(payload: ScanRequest): Promise<ScanReport> {
  const { data } = await scanApi.post<ScanReport>('/api/scans/url', payload)
  return data
}

export async function scanFromFile(file: File, targetName?: string): Promise<ScanReport> {
  const formData = new FormData()
  formData.append('file', file)
  if (targetName) {
    formData.append('targetName', targetName)
  }

  const { data } = await scanApi.post<ScanReport>('/api/scans/file', formData, {
    headers: {
      'Content-Type': 'multipart/form-data'
    }
  })

  return data
}

export async function getScans(): Promise<ScanHistoryItem[]> {
  const { data } = await scanApi.get<ScanHistoryItem[]>('/api/scans')
  return data
}

export async function getScanById(id: string): Promise<ScanReport> {
  const { data } = await scanApi.get<ScanReport>(`/api/scans/${id}`)
  return data
}

export async function deleteScan(id: string): Promise<void> {
  await scanApi.delete(`/api/scans/${id}`)
}

export async function exportScanJson(id: string): Promise<Blob> {
  const { data } = await scanApi.get(`/api/scans/${id}/export`, { responseType: 'blob' })
  return data as Blob
}
