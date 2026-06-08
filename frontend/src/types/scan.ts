export type Severity = 'Low' | 'Medium' | 'High' | 'Critical'

export interface LoginRequest {
  username: string
  password: string
}

export interface RegisterRequest {
  username: string
  password: string
  confirmPassword: string
}

export interface AuthResponse {
  accessToken: string
  tokenType: string
  expiresIn: number
  username: string
  role: string
}

export interface ScanRequest {
  targetName?: string
  openApiUrl: string
}

export interface SecurityIssue {
  ruleCode: string
  severity: Severity
  endpoint: string
  openApiLocation: string
  title: string
  description: string
  recommendation: string
  owaspCategory: string
  owaspTop10Id: string
  owaspTop10Version: string
  owaspTop10Title: string
}

export interface ScanSummary {
  totalIssues: number
  critical: number
  high: number
  medium: number
  low: number
}

export interface ScanReport {
  scanId: string
  score: number
  summary: ScanSummary
  issues: SecurityIssue[]
}

export interface ScanHistoryItem {
  id: string
  targetName: string
  openApiUrl?: string | null
  score: number
  status: string
  createdAt: string
  issuesCount: number
}

export interface AdminUserScanItem {
  id: string
  targetName: string
  openApiUrl?: string | null
  score: number
  status: string
  createdAt: string
  issuesCount: number
}

export interface AdminUserActivity {
  username: string
  role: string
  isActive: boolean
  createdAt: string
  lastLoginAt?: string | null
  scansCount: number
  lastScanAt?: string | null
  scans: AdminUserScanItem[]
}
