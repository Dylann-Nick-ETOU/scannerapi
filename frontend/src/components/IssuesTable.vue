<template>
  <section class="rounded-2xl border border-cyan-800/70 bg-[#032a45]/85 p-9 lg:p-10">
    <div class="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
      <div>
        <h3 class="text-2xl font-semibold">Liste des failles détectées</h3>
        <p class="mt-2 text-cyan-100/80">Détails des vulnérabilités identifiées</p>
      </div>

      <div class="inline-flex w-full max-w-full gap-1 overflow-x-auto rounded-xl border border-cyan-800/70 bg-[#04314e] p-1 lg:w-auto">
        <button
          v-for="mode in viewModes"
          :key="mode.key"
          class="shrink-0 rounded-lg px-4 py-2 text-sm font-medium transition"
          :class="viewMode === mode.key ? 'bg-accent text-night' : 'text-cyan-100/85 hover:bg-cyan-900/40'"
          @click="viewMode = mode.key"
        >
          {{ mode.label }}
        </button>
      </div>
    </div>

    <div class="mt-6 grid gap-3 rounded-2xl border border-cyan-800/70 bg-[#04314e]/70 p-4 md:grid-cols-2 xl:grid-cols-6">
      <label class="flex flex-col gap-2 text-sm text-cyan-100/85 xl:col-span-2">
        <span>Recherche</span>
        <input
          v-model.trim="searchQuery"
          type="text"
          placeholder="endpoint, règle, OWASP, recommandation..."
          class="h-11 rounded-xl border border-cyan-800 bg-[#032a45] px-4 text-cyan-50 outline-none transition placeholder:text-cyan-200/45 focus:border-accent"
        >
      </label>

      <label class="flex flex-col gap-2 text-sm text-cyan-100/85">
        <span>Sévérité</span>
        <select
          v-model="severityFilter"
          class="h-11 rounded-xl border border-cyan-800 bg-[#032a45] px-4 text-cyan-50 outline-none transition focus:border-accent"
        >
          <option value="">Toutes</option>
          <option v-for="severity in severities" :key="severity" :value="severity">{{ severity }}</option>
        </select>
      </label>

      <label class="flex flex-col gap-2 text-sm text-cyan-100/85">
        <span>Confiance</span>
        <select
          v-model="confidenceFilter"
          class="h-11 rounded-xl border border-cyan-800 bg-[#032a45] px-4 text-cyan-50 outline-none transition focus:border-accent"
        >
          <option value="">Toutes</option>
          <option v-for="confidence in confidences" :key="confidence" :value="confidence">{{ confidence }}</option>
        </select>
      </label>

      <label class="flex flex-col gap-2 text-sm text-cyan-100/85">
        <span>OWASP</span>
        <select
          v-model="owaspFilter"
          class="h-11 rounded-xl border border-cyan-800 bg-[#032a45] px-4 text-cyan-50 outline-none transition focus:border-accent"
        >
          <option value="">Tous</option>
          <option v-for="option in owaspOptions" :key="option" :value="option">{{ option }}</option>
        </select>
      </label>

      <label class="flex flex-col gap-2 text-sm text-cyan-100/85">
        <span>État</span>
        <select
          v-model="reviewStatusFilter"
          class="h-11 rounded-xl border border-cyan-800 bg-[#032a45] px-4 text-cyan-50 outline-none transition focus:border-accent"
        >
          <option value="">Tous</option>
          <option v-for="status in reviewStatuses" :key="status" :value="status">{{ reviewStatusLabel(status) }}</option>
        </select>
      </label>
    </div>

    <div class="mt-3 flex flex-col gap-3 text-sm text-cyan-100/80 lg:flex-row lg:items-center lg:justify-between">
      <p>{{ filteredIssues.length }} faille(s) sur {{ props.issues.length }} affichée(s)</p>
      <button
        class="inline-flex h-10 items-center justify-center rounded-xl border border-cyan-700 px-4 text-cyan-100 transition hover:border-accent hover:text-accent disabled:cursor-not-allowed disabled:opacity-50"
        :disabled="!hasActiveFilters"
        @click="resetFilters"
      >
        Réinitialiser les filtres
      </button>
    </div>

    <p v-if="actionError" class="mt-4 rounded-xl border border-critical/50 bg-critical/10 px-4 py-3 text-sm text-critical">
      {{ actionError }}
    </p>

    <div v-if="viewMode === 'detail'" class="mt-6 overflow-x-auto rounded-2xl border border-cyan-800/70">
      <table class="w-full min-w-[1520px] text-left text-lg">
        <thead class="bg-[#04314e]">
          <tr class="border-b border-cyan-800 text-cyan-100">
            <th class="px-4 py-4">Sévérité</th>
            <th class="px-4 py-4">Confiance</th>
            <th class="px-4 py-4">État</th>
            <th class="px-4 py-4">Code règle</th>
            <th class="px-4 py-4">OWASP</th>
            <th class="px-4 py-4">Endpoint</th>
            <th class="px-4 py-4">Chemin spec</th>
            <th class="px-4 py-4">Problème détecté</th>
            <th class="px-4 py-4">Recommandation</th>
            <th class="px-4 py-4">Actions</th>
          </tr>
        </thead>
        <tbody>
          <template v-for="issue in sortedIssues" :key="issue.id">
            <tr class="border-b border-cyan-900/70 align-top">
              <td class="px-4 py-4"><span class="rounded-full border px-3 py-1 text-sm" :class="severityClass(issue.severity)">{{ issue.severity }}</span></td>
              <td class="px-4 py-4"><span class="rounded-full border px-3 py-1 text-sm" :class="confidenceClass(issue.detectionConfidence)">{{ issue.detectionConfidence }}</span></td>
              <td class="px-4 py-4 text-cyan-100/90">
                <div class="space-y-2">
                  <span class="rounded-full border px-3 py-1 text-sm" :class="reviewStatusClass(issue.reviewStatus)">{{ reviewStatusLabel(issue.reviewStatus) }}</span>
                  <p v-if="issue.reviewedAt" class="text-xs text-cyan-100/65">
                    {{ formatReviewMetadata(issue) }}
                  </p>
                  <p v-if="issue.reviewComment" class="text-sm text-cyan-100/80">
                    {{ issue.reviewComment }}
                  </p>
                </div>
              </td>
              <td class="px-4 py-4"><span class="rounded bg-[#355d38] px-3 py-1 font-mono text-accent">{{ issue.ruleCode }}</span></td>
              <td class="px-4 py-4 text-cyan-100/90">{{ owaspLabel(issue) }}</td>
              <td class="px-4 py-4 font-mono text-cyan-100">{{ issue.endpoint }}</td>
              <td class="px-4 py-4 font-mono text-sm text-cyan-100/80 break-all">{{ issue.openApiLocation || '-' }}</td>
              <td class="px-4 py-4 text-cyan-100/90">
                <p>{{ issue.title }}</p>
                <pre v-if="issue.openApiExcerpt" class="mt-3 overflow-x-auto rounded bg-[#082e44] p-3 text-xs text-cyan-100/85 whitespace-pre-wrap break-words">{{ issue.openApiExcerpt }}</pre>
              </td>
              <td class="px-4 py-4 text-cyan-100/90">{{ issue.recommendation }}</td>
              <td class="px-4 py-4">
                <div class="flex min-w-[220px] flex-col gap-2">
                  <button
                    class="rounded-lg border px-3 py-2 text-sm transition"
                    :class="issue.reviewStatus === 'AcceptedRisk' ? 'border-accent bg-accent/15 text-accent' : 'border-cyan-700 text-cyan-100 hover:border-accent hover:text-accent'"
                    :disabled="savingIssueId === issue.id"
                    @click="beginReview(issue, 'AcceptedRisk')"
                  >
                    Risque accepté
                  </button>
                  <button
                    class="rounded-lg border px-3 py-2 text-sm transition"
                    :class="issue.reviewStatus === 'FalsePositive' ? 'border-safe bg-safe/15 text-safe' : 'border-cyan-700 text-cyan-100 hover:border-safe hover:text-safe'"
                    :disabled="savingIssueId === issue.id"
                    @click="beginReview(issue, 'FalsePositive')"
                  >
                    Faux positif
                  </button>
                  <button
                    class="rounded-lg border border-cyan-700 px-3 py-2 text-sm text-cyan-100 transition hover:border-warning hover:text-warning disabled:cursor-not-allowed disabled:opacity-50"
                    :disabled="issue.reviewStatus === 'Open' || savingIssueId === issue.id"
                    @click="reopenIssue(issue)"
                  >
                    Rouvrir
                  </button>
                </div>
              </td>
            </tr>

            <tr v-if="reviewDraft?.issueId === issue.id" class="border-b border-cyan-900/70 bg-[#02263d]/80">
              <td colspan="10" class="px-4 py-4">
                <div class="grid gap-4 lg:grid-cols-[1fr_auto] lg:items-end">
                  <label class="flex flex-col gap-2 text-sm text-cyan-100/85">
                    <span>Commentaire {{ reviewStatusLabel(reviewDraft.status).toLowerCase() }}</span>
                    <textarea
                      v-model.trim="reviewDraft.comment"
                      rows="3"
                      maxlength="1000"
                      class="rounded-xl border border-cyan-800 bg-[#032a45] px-4 py-3 text-cyan-50 outline-none transition placeholder:text-cyan-200/45 focus:border-accent"
                      placeholder="Commentaire optionnel conservé dans le rapport..."
                    />
                  </label>

                  <div class="flex flex-wrap gap-2 lg:justify-end">
                    <button
                      class="rounded-lg border border-cyan-700 px-4 py-2 text-sm text-cyan-100 transition hover:border-cyan-400 hover:text-cyan-50"
                      :disabled="savingIssueId === issue.id"
                      @click="cancelReview"
                    >
                      Annuler
                    </button>
                    <button
                      class="rounded-lg bg-accent px-4 py-2 text-sm font-medium text-night transition hover:brightness-110 disabled:cursor-not-allowed disabled:opacity-60"
                      :disabled="savingIssueId === issue.id"
                      @click="saveReview"
                    >
                      {{ savingIssueId === issue.id ? 'Enregistrement...' : 'Enregistrer' }}
                    </button>
                  </div>
                </div>
              </td>
            </tr>
          </template>
        </tbody>
      </table>
      <div v-if="sortedIssues.length === 0" class="px-6 py-8 text-sm text-cyan-100/75">
        Aucun finding ne correspond aux filtres sélectionnés.
      </div>
    </div>

    <div v-else-if="viewMode === 'owasp'" class="mt-6 overflow-x-auto rounded-2xl border border-cyan-800/70">
      <table class="w-full min-w-[1040px] text-left text-lg">
        <thead class="bg-[#04314e]">
          <tr class="border-b border-cyan-800 text-cyan-100">
            <th class="px-4 py-4">Référence OWASP</th>
            <th class="px-4 py-4">Failles</th>
            <th class="px-4 py-4">Endpoints</th>
            <th class="px-4 py-4">Sévérité max</th>
            <th class="px-4 py-4">Confiance max</th>
            <th class="px-4 py-4">Règles</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="group in owaspGroups" :key="group.key" class="border-b border-cyan-900/70">
            <td class="px-4 py-4 text-cyan-100">{{ group.label }}</td>
            <td class="px-4 py-4 text-cyan-100/90">{{ group.count }}</td>
            <td class="px-4 py-4 text-cyan-100/90">{{ group.endpoints.length }}</td>
            <td class="px-4 py-4"><span class="rounded-full border px-3 py-1 text-sm" :class="severityClass(group.worstSeverity)">{{ group.worstSeverity }}</span></td>
            <td class="px-4 py-4"><span class="rounded-full border px-3 py-1 text-sm" :class="confidenceClass(group.highestConfidence)">{{ group.highestConfidence }}</span></td>
            <td class="px-4 py-4 font-mono text-sm text-cyan-100/85">{{ group.ruleCodes.join(', ') }}</td>
          </tr>
        </tbody>
      </table>
      <div v-if="owaspGroups.length === 0" class="px-6 py-8 text-sm text-cyan-100/75">
        Aucun regroupement OWASP ne correspond aux filtres sélectionnés.
      </div>
    </div>

    <div v-else class="mt-6 overflow-x-auto rounded-2xl border border-cyan-800/70">
      <table class="w-full min-w-[1140px] text-left text-lg">
        <thead class="bg-[#04314e]">
          <tr class="border-b border-cyan-800 text-cyan-100">
            <th class="px-4 py-4">Endpoint</th>
            <th class="px-4 py-4">Failles</th>
            <th class="px-4 py-4">OWASP</th>
            <th class="px-4 py-4">Sévérité max</th>
            <th class="px-4 py-4">Confiance max</th>
            <th class="px-4 py-4">Règles</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="group in endpointGroups" :key="group.key" class="border-b border-cyan-900/70">
            <td class="px-4 py-4 font-mono text-cyan-100">{{ group.label }}</td>
            <td class="px-4 py-4 text-cyan-100/90">{{ group.count }}</td>
            <td class="px-4 py-4 text-sm text-cyan-100/85">{{ group.owaspReferences.join(', ') }}</td>
            <td class="px-4 py-4"><span class="rounded-full border px-3 py-1 text-sm" :class="severityClass(group.worstSeverity)">{{ group.worstSeverity }}</span></td>
            <td class="px-4 py-4"><span class="rounded-full border px-3 py-1 text-sm" :class="confidenceClass(group.highestConfidence)">{{ group.highestConfidence }}</span></td>
            <td class="px-4 py-4 font-mono text-sm text-cyan-100/85">{{ group.ruleCodes.join(', ') }}</td>
          </tr>
        </tbody>
      </table>
      <div v-if="endpointGroups.length === 0" class="px-6 py-8 text-sm text-cyan-100/75">
        Aucun regroupement endpoint ne correspond aux filtres sélectionnés.
      </div>
    </div>
  </section>
</template>

<script setup lang="ts">
import axios from 'axios'
import { computed, ref } from 'vue'
import { updateIssueReview } from '../services/scanApi'
import type { DetectionConfidence, ReviewStatus, SecurityIssue, Severity } from '../types/scan'
import { groupIssuesByEndpoint, groupIssuesByOwasp, owaspLabel, reviewStatusLabel, sortIssues } from '../utils/issuePresentation'

const props = defineProps<{
  scanId: string
  issues: SecurityIssue[]
}>()

const emit = defineEmits<{
  (e: 'issue-updated', issue: SecurityIssue): void
  (e: 'action-error', message: string): void
}>()

const viewMode = ref<'detail' | 'owasp' | 'endpoint'>('detail')
const searchQuery = ref('')
const severityFilter = ref<Severity | ''>('')
const confidenceFilter = ref<DetectionConfidence | ''>('')
const owaspFilter = ref('')
const reviewStatusFilter = ref<ReviewStatus | ''>('')
const reviewDraft = ref<{ issueId: string; status: Exclude<ReviewStatus, 'Open'>; comment: string } | null>(null)
const savingIssueId = ref<string | null>(null)
const actionError = ref('')
const viewModes = [
  { key: 'detail', label: 'Détail' },
  { key: 'owasp', label: 'Par OWASP' },
  { key: 'endpoint', label: 'Par endpoint' }
] as const

const severities: Severity[] = ['Critical', 'High', 'Medium', 'Low']
const confidences: DetectionConfidence[] = ['High', 'Medium', 'Low']
const reviewStatuses: ReviewStatus[] = ['Open', 'AcceptedRisk', 'FalsePositive']

const owaspOptions = computed(() => {
  return [...new Set(props.issues.map(issue => owaspLabel(issue)))].sort((left, right) => left.localeCompare(right, 'fr'))
})

const filteredIssues = computed(() => {
  const query = normalize(searchQuery.value)

  return props.issues.filter(issue => {
    if (severityFilter.value && issue.severity !== severityFilter.value) {
      return false
    }

    if (confidenceFilter.value && issue.detectionConfidence !== confidenceFilter.value) {
      return false
    }

    if (reviewStatusFilter.value && issue.reviewStatus !== reviewStatusFilter.value) {
      return false
    }

    const issueOwaspLabel = owaspLabel(issue)
    if (owaspFilter.value && issueOwaspLabel !== owaspFilter.value) {
      return false
    }

    if (!query) {
      return true
    }

    const haystack = [
      issue.ruleCode,
      issue.endpoint,
      issue.title,
      issue.description,
      issue.recommendation,
      issue.openApiLocation,
      issue.reviewComment,
      reviewStatusLabel(issue.reviewStatus),
      issueOwaspLabel
    ]
      .join(' ')
      .toLowerCase()

    return haystack.includes(query)
  })
})

const sortedIssues = computed(() => sortIssues(filteredIssues.value))
const owaspGroups = computed(() => groupIssuesByOwasp(filteredIssues.value))
const endpointGroups = computed(() => groupIssuesByEndpoint(filteredIssues.value))
const hasActiveFilters = computed(() =>
  Boolean(searchQuery.value || severityFilter.value || confidenceFilter.value || owaspFilter.value || reviewStatusFilter.value)
)

function severityClass(severity: SecurityIssue['severity']): string {
  if (severity === 'Critical') return 'border-critical/50 text-critical bg-critical/10'
  if (severity === 'High') return 'border-warning/50 text-warning bg-warning/10'
  if (severity === 'Medium') return 'border-accent/50 text-accent bg-accent/10'
  return 'border-cyan-500/50 text-cyan-200 bg-cyan-500/10'
}

function confidenceClass(confidence: SecurityIssue['detectionConfidence']): string {
  if (confidence === 'High') return 'border-safe/50 text-safe bg-safe/10'
  if (confidence === 'Medium') return 'border-warning/50 text-warning bg-warning/10'
  return 'border-cyan-500/50 text-cyan-200 bg-cyan-500/10'
}

function reviewStatusClass(status: SecurityIssue['reviewStatus']): string {
  if (status === 'AcceptedRisk') return 'border-accent/50 text-accent bg-accent/10'
  if (status === 'FalsePositive') return 'border-safe/50 text-safe bg-safe/10'
  return 'border-critical/50 text-critical bg-critical/10'
}

function beginReview(issue: SecurityIssue, status: Exclude<ReviewStatus, 'Open'>) {
  actionError.value = ''
  reviewDraft.value = {
    issueId: issue.id,
    status,
    comment: issue.reviewStatus === status ? issue.reviewComment : ''
  }
}

function cancelReview() {
  reviewDraft.value = null
}

async function saveReview() {
  if (!reviewDraft.value) {
    return
  }

  const draft = { ...reviewDraft.value }
  savingIssueId.value = draft.issueId
  actionError.value = ''

  try {
    const updatedIssue = await updateIssueReview(props.scanId, draft.issueId, {
      status: draft.status,
      comment: draft.comment
    })
    emit('issue-updated', updatedIssue)
    reviewDraft.value = null
  } catch (err: unknown) {
    const message = extractActionError(err)
    actionError.value = message
    emit('action-error', message)
  } finally {
    savingIssueId.value = null
  }
}

async function reopenIssue(issue: SecurityIssue) {
  savingIssueId.value = issue.id
  actionError.value = ''

  try {
    const updatedIssue = await updateIssueReview(props.scanId, issue.id, { status: 'Open' })
    emit('issue-updated', updatedIssue)
    if (reviewDraft.value?.issueId === issue.id) {
      reviewDraft.value = null
    }
  } catch (err: unknown) {
    const message = extractActionError(err)
    actionError.value = message
    emit('action-error', message)
  } finally {
    savingIssueId.value = null
  }
}

function formatReviewMetadata(issue: SecurityIssue): string {
  const date = issue.reviewedAt
    ? new Intl.DateTimeFormat('fr-FR', {
        dateStyle: 'short',
        timeStyle: 'short'
      }).format(new Date(issue.reviewedAt))
    : null

  if (date && issue.reviewedBy) {
    return `Traité par ${issue.reviewedBy} le ${date}`
  }

  if (date) {
    return `Traité le ${date}`
  }

  return ''
}

function resetFilters() {
  searchQuery.value = ''
  severityFilter.value = ''
  confidenceFilter.value = ''
  owaspFilter.value = ''
  reviewStatusFilter.value = ''
}

function extractActionError(err: unknown): string {
  if (axios.isAxiosError(err)) {
    const message = err.response?.data?.message
    if (typeof message === 'string' && message.trim().length > 0) {
      return message
    }
  }

  return 'Impossible de mettre à jour cet état de revue.'
}

function normalize(value: string): string {
  return value.trim().toLowerCase()
}
</script>
