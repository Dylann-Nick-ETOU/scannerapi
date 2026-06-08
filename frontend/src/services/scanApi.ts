import axios from 'axios'
import type { AdminUserActivity, AuthResponse, LoginRequest, RegisterRequest, ScanHistoryItem, ScanReport, ScanRequest, SecurityIssue, UpdateIssueReviewRequest } from '../types/scan'

export const scanApi = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? '/api'
})

const storageKey = 'api-security-scanner.access-token'

let accessToken: string | null = loadStoredToken()

scanApi.interceptors.request.use(async (config) => {
  if (config.url?.startsWith('/health') || config.url?.startsWith('/auth/')) {
    return config
  }

  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`
  }
  return config
})

scanApi.interceptors.response.use(undefined, (error) => {
  if (axios.isAxiosError(error) && error.response?.status === 401) {
    clearAccessToken()
  }

  return Promise.reject(error)
})

export function getAccessToken(): string | null {
  return accessToken
}

export function readStoredAuthState(): AuthResponse | null {
  if (!accessToken) {
    return null
  }

  try {
    const payload = JSON.parse(atob(accessToken.split('.')[1]))
    const username =
      payload.unique_name ??
      payload.name ??
      payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ??
      payload.sub ??
      payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ??
      'Session active'

    const role =
      payload.role ??
      payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ??
      (Array.isArray(payload.roles) ? payload.roles[0] : payload.roles) ??
      'User'

    return {
      accessToken,
      tokenType: 'Bearer',
      expiresIn: 0,
      username,
      role
    }
  } catch {
    return {
      accessToken,
      tokenType: 'Bearer',
      expiresIn: 0,
      username: 'Session active',
      role: 'User'
    }
  }
}

export function setAccessToken(token: string): void {
  accessToken = token.trim()

  if (typeof window !== 'undefined') {
    window.localStorage.setItem(storageKey, accessToken)
  }
}

export function clearAccessToken(): void {
  accessToken = null

  if (typeof window !== 'undefined') {
    window.localStorage.removeItem(storageKey)
  }
}

export async function login(payload: LoginRequest): Promise<AuthResponse> {
  const { data } = await scanApi.post<AuthResponse>('/auth/login', payload)
  setAccessToken(data.accessToken)
  return data
}

export async function register(payload: RegisterRequest): Promise<AuthResponse> {
  const { data } = await scanApi.post<AuthResponse>('/auth/register', payload)
  setAccessToken(data.accessToken)
  return data
}

function loadStoredToken(): string | null {
  if (typeof window === 'undefined') {
    return null
  }

  const token = window.localStorage.getItem(storageKey)?.trim()
  return token ? token : null
}

export async function scanFromUrl(payload: ScanRequest): Promise<ScanReport> {
  const { data } = await scanApi.post<ScanReport>('/scans/url', payload)
  return data
}

export async function scanFromFile(file: File, targetName?: string): Promise<ScanReport> {
  const formData = new FormData()
  formData.append('file', file)
  if (targetName) {
    formData.append('targetName', targetName)
  }

  const { data } = await scanApi.post<ScanReport>('/scans/file', formData, {
    headers: {
      'Content-Type': 'multipart/form-data'
    }
  })

  return data
}

export async function getScans(): Promise<ScanHistoryItem[]> {
  const { data } = await scanApi.get<ScanHistoryItem[]>('/scans')
  return data
}

export async function getScanById(id: string): Promise<ScanReport> {
  const { data } = await scanApi.get<ScanReport>(`/scans/${id}`)
  return data
}

export async function updateIssueReview(scanId: string, issueId: string, payload: UpdateIssueReviewRequest): Promise<SecurityIssue> {
  const { data } = await scanApi.patch<SecurityIssue>(`/scans/${scanId}/issues/${issueId}/review`, payload)
  return data
}

export async function deleteScan(id: string): Promise<void> {
  await scanApi.delete(`/scans/${id}`)
}

export async function exportScanJson(id: string): Promise<Blob> {
  const { data } = await scanApi.get(`/scans/${id}/export`, { responseType: 'blob' })
  return data as Blob
}

export async function getAdminUsers(): Promise<AdminUserActivity[]> {
  const { data } = await scanApi.get<AdminUserActivity[]>('/admin/users')
  return data
}

export async function deactivateUser(username: string): Promise<void> {
  await scanApi.post(`/admin/users/${encodeURIComponent(username)}/deactivate`)
}
