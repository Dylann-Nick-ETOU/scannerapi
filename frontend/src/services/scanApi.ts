import axios from 'axios'
import type { ScanReport, ScanRequest } from '../types/scan'

export const scanApi = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:8082'
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
