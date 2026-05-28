export type Severity = 'Low' | 'Medium' | 'High' | 'Critical'

export interface SecurityIssue {
  ruleCode: string
  severity: Severity
  endpoint: string
  title: string
  description: string
  recommendation: string
  owaspCategory: string
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
