export type Severity = 'Low' | 'Medium' | 'High' | 'Critical'
export type DetectionConfidence = 'Low' | 'Medium' | 'High'
export type ReviewStatus = 'Open' | 'AcceptedRisk' | 'FalsePositive'

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
  id: string
  ruleCode: string
  severity: Severity
  detectionConfidence: DetectionConfidence
  reviewStatus: ReviewStatus
  reviewComment: string
  reviewedAt?: string | null
  reviewedBy: string
  endpoint: string
  openApiLocation: string
  openApiExcerpt: string
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

export interface UpdateIssueReviewRequest {
  status: ReviewStatus
  comment?: string
}

export interface ComparedScan {
  scanId: string
  targetName: string
  openApiUrl?: string | null
  createdAt: string
  score: number
  summary: ScanSummary
}

export interface ScanComparisonSummary {
  newIssuesCount: number
  resolvedIssuesCount: number
  unchangedIssuesCount: number
}

export interface ScanComparison {
  baseline: ComparedScan
  current: ComparedScan
  scoreDelta: number
  totalIssuesDelta: number
  summary: ScanComparisonSummary
  newIssues: SecurityIssue[]
  resolvedIssues: SecurityIssue[]
  unchangedIssues: SecurityIssue[]
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

export interface AdminAuditLog {
  id: string
  adminUsername: string
  actionType: string
  targetUsername?: string | null
  targetScanId?: string | null
  details: string
  createdAt: string
}
