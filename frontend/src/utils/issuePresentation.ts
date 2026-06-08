import type { DetectionConfidence, ReviewStatus, SecurityIssue, Severity } from '../types/scan'

export interface IssueGroup {
  key: string
  label: string
  count: number
  worstSeverity: Severity
  highestConfidence: DetectionConfidence
  endpoints: string[]
  owaspReferences: string[]
  ruleCodes: string[]
  issues: SecurityIssue[]
}

const severityOrder: Record<Severity, number> = {
  Critical: 4,
  High: 3,
  Medium: 2,
  Low: 1
}

const confidenceOrder: Record<DetectionConfidence, number> = {
  High: 3,
  Medium: 2,
  Low: 1
}

const reviewStatusOrder: Record<ReviewStatus, number> = {
  Open: 3,
  AcceptedRisk: 2,
  FalsePositive: 1
}

export function sortIssues(issues: SecurityIssue[]): SecurityIssue[] {
  return [...issues].sort((left, right) => {
    const reviewDelta = reviewStatusRank(right.reviewStatus) - reviewStatusRank(left.reviewStatus)
    if (reviewDelta !== 0) {
      return reviewDelta
    }

    const severityDelta = confidenceAwareSeverityRank(right) - confidenceAwareSeverityRank(left)
    if (severityDelta !== 0) {
      return severityDelta
    }

    const confidenceDelta = confidenceRank(right.detectionConfidence) - confidenceRank(left.detectionConfidence)
    if (confidenceDelta !== 0) {
      return confidenceDelta
    }

    const endpointDelta = left.endpoint.localeCompare(right.endpoint, 'fr')
    if (endpointDelta !== 0) {
      return endpointDelta
    }

    return left.ruleCode.localeCompare(right.ruleCode, 'fr')
  })
}

export function owaspLabel(issue: SecurityIssue): string {
  if (issue.owaspTop10Id && issue.owaspTop10Version && issue.owaspTop10Title) {
    return `${issue.owaspTop10Id}:${issue.owaspTop10Version} - ${issue.owaspTop10Title}`
  }

  return issue.owaspCategory
}

export function groupIssuesByOwasp(issues: SecurityIssue[]): IssueGroup[] {
  return buildGroups(sortIssues(issues), issue => owaspLabel(issue))
}

export function groupIssuesByEndpoint(issues: SecurityIssue[]): IssueGroup[] {
  return buildGroups(sortIssues(issues), issue => issue.endpoint)
}

export function severityRank(severity: Severity): number {
  return severityOrder[severity]
}

export function confidenceRank(confidence: DetectionConfidence): number {
  return confidenceOrder[confidence]
}

export function reviewStatusRank(status: ReviewStatus): number {
  return reviewStatusOrder[status]
}

export function reviewStatusLabel(status: ReviewStatus): string {
  if (status === 'AcceptedRisk') return 'Risque accepté'
  if (status === 'FalsePositive') return 'Faux positif'
  return 'Ouvert'
}

function confidenceAwareSeverityRank(issue: SecurityIssue): number {
  return severityRank(issue.severity) * 10 + confidenceRank(issue.detectionConfidence)
}

function buildGroups(sortedIssues: SecurityIssue[], keySelector: (issue: SecurityIssue) => string): IssueGroup[] {
  const groups = new Map<string, SecurityIssue[]>()

  for (const issue of sortedIssues) {
    const key = keySelector(issue)
    const bucket = groups.get(key)
    if (bucket) {
      bucket.push(issue)
    } else {
      groups.set(key, [issue])
    }
  }

  return [...groups.entries()]
    .map(([key, groupIssues]) => ({
      key,
      label: key,
      count: groupIssues.length,
      worstSeverity: groupIssues.reduce((worst, issue) => severityRank(issue.severity) > severityRank(worst) ? issue.severity : worst, groupIssues[0].severity),
      highestConfidence: groupIssues.reduce((highest, issue) => confidenceRank(issue.detectionConfidence) > confidenceRank(highest) ? issue.detectionConfidence : highest, groupIssues[0].detectionConfidence),
      endpoints: uniqueSorted(groupIssues.map(issue => issue.endpoint)),
      owaspReferences: uniqueSorted(groupIssues.map(issue => owaspLabel(issue))),
      ruleCodes: uniqueSorted(groupIssues.map(issue => issue.ruleCode)),
      issues: groupIssues
    }))
    .sort((left, right) => {
      const severityDelta = severityRank(right.worstSeverity) - severityRank(left.worstSeverity)
      if (severityDelta !== 0) {
        return severityDelta
      }

      const countDelta = right.count - left.count
      if (countDelta !== 0) {
        return countDelta
      }

      return left.label.localeCompare(right.label, 'fr')
    })
}

function uniqueSorted(values: string[]): string[] {
  return [...new Set(values)].sort((left, right) => left.localeCompare(right, 'fr'))
}
