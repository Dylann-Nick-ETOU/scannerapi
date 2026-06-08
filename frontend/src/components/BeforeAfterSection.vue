<template>
  <section class="space-y-6 rounded-2xl border border-cyan-800/70 bg-[#032a45]/85 p-8">
    <div class="flex flex-col gap-3 lg:flex-row lg:items-end lg:justify-between">
      <div>
        <h3 class="text-2xl font-semibold">Avant / Après</h3>
        <p class="mt-2 text-cyan-100/80">Comparez deux scans pour voir ce qui a été corrigé et ce qui reste exposé.</p>
      </div>
      <div class="text-sm text-cyan-100/70">
        Matching strict: <span class="font-mono text-cyan-50">RuleCode + Endpoint + OpenApiLocation</span>
      </div>
    </div>

    <div class="grid gap-4 rounded-2xl border border-cyan-800/70 bg-[#04314e]/70 p-4 lg:grid-cols-[1fr_1fr_auto] lg:items-end">
      <label class="flex flex-col gap-2 text-sm text-cyan-100/85">
        <span>Scan courant</span>
        <select
          v-model="selectedCurrentId"
          class="h-11 rounded-xl border border-cyan-800 bg-[#032a45] px-4 text-cyan-50 outline-none transition focus:border-accent"
        >
          <option value="">Sélectionner un scan</option>
          <option v-for="item in items" :key="item.id" :value="item.id">{{ formatScanOption(item) }}</option>
        </select>
      </label>

      <label class="flex flex-col gap-2 text-sm text-cyan-100/85">
        <span>Scan de référence</span>
        <select
          v-model="selectedBaselineId"
          class="h-11 rounded-xl border border-cyan-800 bg-[#032a45] px-4 text-cyan-50 outline-none transition focus:border-accent"
        >
          <option value="">Sélectionner un scan</option>
          <option v-for="item in items" :key="item.id" :value="item.id">{{ formatScanOption(item) }}</option>
        </select>
      </label>

      <button
        class="inline-flex h-11 items-center justify-center rounded-xl bg-accent px-5 text-sm font-medium text-night transition hover:brightness-110 disabled:cursor-not-allowed disabled:opacity-60"
        :disabled="!canCompare || loading"
        @click="submitCompare"
      >
        {{ loading ? 'Comparaison...' : 'Comparer' }}
      </button>
    </div>

    <p v-if="error" class="rounded-xl border border-critical/50 bg-critical/10 px-4 py-3 text-sm text-critical">
      {{ error }}
    </p>

    <div v-if="items.length < 2" class="rounded-2xl border border-cyan-800/70 bg-[#04314e]/70 px-5 py-4 text-sm text-cyan-100/75">
      Il faut au moins deux scans dans l'historique pour lancer une comparaison.
    </div>

    <template v-if="comparison">
      <div class="grid gap-3 rounded-2xl border border-cyan-800/70 bg-[#04314e]/70 p-4 md:grid-cols-2 xl:grid-cols-5">
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
      </div>

      <div class="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
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

        <div class="flex flex-col gap-3 text-sm text-cyan-100/80 lg:flex-row lg:items-center">
          <p>
            {{ filteredNewIssues.length + filteredResolvedIssues.length + filteredUnchangedIssues.length }}
            finding(s) affiché(s) sur
            {{ comparison.summary.newIssuesCount + comparison.summary.resolvedIssuesCount + comparison.summary.unchangedIssuesCount }}
          </p>
          <button
            class="inline-flex h-10 items-center justify-center rounded-xl border border-cyan-700 px-4 text-cyan-100 transition hover:border-accent hover:text-accent disabled:cursor-not-allowed disabled:opacity-50"
            :disabled="!hasActiveFilters"
            @click="resetFilters"
          >
            Réinitialiser les filtres
          </button>
        </div>
      </div>

      <div class="grid gap-4 lg:grid-cols-4">
        <article class="rounded-2xl border border-cyan-800/70 bg-[#04314e] p-5">
          <p class="text-sm text-cyan-100/70">Delta score</p>
          <p class="mt-2 text-3xl font-semibold" :class="comparison.scoreDelta >= 0 ? 'text-safe' : 'text-critical'">
            {{ signed(comparison.scoreDelta) }}
          </p>
        </article>
        <article class="rounded-2xl border border-cyan-800/70 bg-[#04314e] p-5">
          <p class="text-sm text-cyan-100/70">Nouvelles failles</p>
          <p class="mt-2 text-3xl font-semibold text-critical">{{ comparison.summary.newIssuesCount }}</p>
        </article>
        <article class="rounded-2xl border border-cyan-800/70 bg-[#04314e] p-5">
          <p class="text-sm text-cyan-100/70">Failles corrigées</p>
          <p class="mt-2 text-3xl font-semibold text-safe">{{ comparison.summary.resolvedIssuesCount }}</p>
        </article>
        <article class="rounded-2xl border border-cyan-800/70 bg-[#04314e] p-5">
          <p class="text-sm text-cyan-100/70">Toujours présentes</p>
          <p class="mt-2 text-3xl font-semibold text-accent">{{ comparison.summary.unchangedIssuesCount }}</p>
        </article>
      </div>

      <div class="grid gap-6 lg:grid-cols-2">
        <article class="rounded-2xl border border-critical/40 bg-[#042f4b] p-6">
          <div class="flex items-start justify-between gap-4">
            <div>
              <p class="text-sm text-cyan-100/70">Référence</p>
              <h4 class="mt-1 text-xl font-semibold">{{ comparison.baseline.targetName }}</h4>
              <p class="mt-1 text-sm text-cyan-100/75">{{ formatDate(comparison.baseline.createdAt) }}</p>
            </div>
            <span class="rounded-full border border-cyan-700 px-3 py-1 text-sm text-cyan-100">Score {{ comparison.baseline.score }}</span>
          </div>
          <p class="mt-4 text-sm text-cyan-100/75">
            {{ comparison.baseline.summary.totalIssues }} findings, dont {{ comparison.baseline.summary.critical }} critiques et {{ comparison.baseline.summary.high }} high.
          </p>
        </article>

        <article class="rounded-2xl border border-safe/40 bg-[#042f4b] p-6">
          <div class="flex items-start justify-between gap-4">
            <div>
              <p class="text-sm text-cyan-100/70">Courant</p>
              <h4 class="mt-1 text-xl font-semibold">{{ comparison.current.targetName }}</h4>
              <p class="mt-1 text-sm text-cyan-100/75">{{ formatDate(comparison.current.createdAt) }}</p>
            </div>
            <span class="rounded-full border border-cyan-700 px-3 py-1 text-sm text-cyan-100">Score {{ comparison.current.score }}</span>
          </div>
          <p class="mt-4 text-sm text-cyan-100/75">
            {{ comparison.current.summary.totalIssues }} findings, dont {{ comparison.current.summary.critical }} critiques et {{ comparison.current.summary.high }} high.
          </p>
        </article>
      </div>

      <div class="grid gap-6 xl:grid-cols-3">
        <article class="rounded-2xl border border-critical/40 bg-[#042f4b] p-6">
          <div class="mb-4 flex items-center justify-between gap-3">
            <h4 class="text-xl font-semibold text-critical">Nouvelles failles</h4>
            <span class="rounded-full border border-critical/40 px-3 py-1 text-sm text-critical">{{ filteredNewIssues.length }} / {{ comparison.summary.newIssuesCount }}</span>
          </div>
          <IssueRenderer
            :issues="filteredNewIssues"
            :mode="viewMode"
            accent="critical"
            :empty-text="emptyText('Aucune nouvelle faille détectée.')"
          />
        </article>

        <article class="rounded-2xl border border-safe/40 bg-[#042f4b] p-6">
          <div class="mb-4 flex items-center justify-between gap-3">
            <h4 class="text-xl font-semibold text-safe">Failles corrigées</h4>
            <span class="rounded-full border border-safe/40 px-3 py-1 text-sm text-safe">{{ filteredResolvedIssues.length }} / {{ comparison.summary.resolvedIssuesCount }}</span>
          </div>
          <IssueRenderer
            :issues="filteredResolvedIssues"
            :mode="viewMode"
            accent="safe"
            :empty-text="emptyText('Aucune faille résolue dans cet intervalle.')"
          />
        </article>

        <article class="rounded-2xl border border-accent/40 bg-[#042f4b] p-6">
          <div class="mb-4 flex items-center justify-between gap-3">
            <h4 class="text-xl font-semibold text-accent">Toujours présentes</h4>
            <span class="rounded-full border border-accent/40 px-3 py-1 text-sm text-accent">{{ filteredUnchangedIssues.length }} / {{ comparison.summary.unchangedIssuesCount }}</span>
          </div>
          <IssueRenderer
            :issues="filteredUnchangedIssues"
            :mode="viewMode"
            accent="accent"
            :empty-text="emptyText('Aucune faille persistante.')"
          />
        </article>
      </div>
    </template>

    <div v-else class="rounded-2xl border border-cyan-800/70 bg-[#04314e]/70 px-5 py-4 text-sm text-cyan-100/75">
      Sélectionnez deux scans depuis l'historique pour afficher les différences.
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed, defineComponent, h, ref, watch } from 'vue'
import type { PropType } from 'vue'
import type { DetectionConfidence, SecurityIssue, Severity, ScanComparison, ScanHistoryItem } from '../types/scan'
import { confidenceClassName, groupIssuesByEndpoint, groupIssuesByOwasp, IssueGroup, owaspLabel, severityClassName, sortIssues } from '../utils/issuePresentation'

const props = defineProps<{
  items: ScanHistoryItem[]
  comparison: ScanComparison | null
  loading: boolean
  error?: string
  currentScanId?: string | null
  baselineScanId?: string | null
}>()

const emit = defineEmits<{
  compare: [currentScanId: string, baselineScanId: string]
}>()

const selectedCurrentId = ref(props.currentScanId ?? '')
const selectedBaselineId = ref(props.baselineScanId ?? '')
const searchQuery = ref('')
const severityFilter = ref<Severity | ''>('')
const confidenceFilter = ref<DetectionConfidence | ''>('')
const owaspFilter = ref('')
const viewMode = ref<'detail' | 'owasp' | 'endpoint'>('detail')

const severities: Severity[] = ['Critical', 'High', 'Medium', 'Low']
const confidences: DetectionConfidence[] = ['High', 'Medium', 'Low']
const viewModes = [
  { key: 'detail', label: 'Détail' },
  { key: 'owasp', label: 'Par OWASP' },
  { key: 'endpoint', label: 'Par endpoint' }
] as const

watch(() => props.currentScanId, value => {
  selectedCurrentId.value = value ?? ''
})

watch(() => props.baselineScanId, value => {
  selectedBaselineId.value = value ?? ''
})

const canCompare = computed(() =>
  Boolean(selectedCurrentId.value && selectedBaselineId.value && selectedCurrentId.value !== selectedBaselineId.value)
)

const comparisonIssues = computed(() => {
  if (!props.comparison) {
    return []
  }

  return [
    ...props.comparison.newIssues,
    ...props.comparison.resolvedIssues,
    ...props.comparison.unchangedIssues
  ]
})

const owaspOptions = computed(() => {
  return [...new Set(comparisonIssues.value.map(issue => owaspLabel(issue)))].sort((left, right) => left.localeCompare(right, 'fr'))
})

const hasActiveFilters = computed(() =>
  Boolean(searchQuery.value || severityFilter.value || confidenceFilter.value || owaspFilter.value)
)

const filteredNewIssues = computed(() => filterIssues(props.comparison?.newIssues ?? []))
const filteredResolvedIssues = computed(() => filterIssues(props.comparison?.resolvedIssues ?? []))
const filteredUnchangedIssues = computed(() => filterIssues(props.comparison?.unchangedIssues ?? []))

function submitCompare() {
  if (!canCompare.value) {
    return
  }

  emit('compare', selectedCurrentId.value, selectedBaselineId.value)
}

function formatDate(value: string): string {
  return new Date(value).toLocaleString('fr-FR')
}

function formatScanOption(item: ScanHistoryItem): string {
  return `${item.targetName} · ${formatDate(item.createdAt)} · score ${item.score}`
}

function signed(value: number): string {
  return value > 0 ? `+${value}` : value.toString()
}

function resetFilters() {
  searchQuery.value = ''
  severityFilter.value = ''
  confidenceFilter.value = ''
  owaspFilter.value = ''
}

function emptyText(defaultText: string): string {
  return hasActiveFilters.value ? 'Aucune faille ne correspond aux filtres sélectionnés.' : defaultText
}

function filterIssues(issues: SecurityIssue[]): SecurityIssue[] {
  const query = normalize(searchQuery.value)

  return sortIssues(issues).filter(issue => {
    if (severityFilter.value && issue.severity !== severityFilter.value) {
      return false
    }

    if (confidenceFilter.value && issue.detectionConfidence !== confidenceFilter.value) {
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
      issueOwaspLabel
    ]
      .join(' ')
      .toLowerCase()

    return haystack.includes(query)
  })
}

function normalize(value: string): string {
  return value.trim().toLowerCase()
}

const IssueRenderer = defineComponent({
  name: 'ComparisonIssueRenderer',
  props: {
    issues: {
      type: Array as PropType<SecurityIssue[]>,
      required: true
    },
    mode: {
      type: String as PropType<'detail' | 'owasp' | 'endpoint'>,
      required: true
    },
    accent: {
      type: String as PropType<'critical' | 'safe' | 'accent'>,
      required: true
    },
    emptyText: {
      type: String,
      required: true
    }
  },
  setup(componentProps) {
    const orderedIssues = computed(() => sortIssues(componentProps.issues))
    const owaspGroups = computed(() => groupIssuesByOwasp(componentProps.issues))
    const endpointGroups = computed(() => groupIssuesByEndpoint(componentProps.issues))

    return () => {
      if (orderedIssues.value.length === 0) {
        return h('p', { class: 'text-sm text-cyan-100/70' }, componentProps.emptyText)
      }

      if (componentProps.mode === 'owasp') {
        return h(GroupList, {
          groups: owaspGroups.value,
          label: 'Réf. OWASP',
          accent: componentProps.accent
        })
      }

      if (componentProps.mode === 'endpoint') {
        return h(GroupList, {
          groups: endpointGroups.value,
          label: 'Endpoint',
          accent: componentProps.accent
        })
      }

      return h(IssueList, {
        issues: orderedIssues.value
      })
    }
  }
})

const IssueList = defineComponent({
  name: 'ComparisonIssueList',
  props: {
    issues: {
      type: Array as PropType<SecurityIssue[]>,
      required: true
    }
  },
  setup(componentProps) {
    return () =>
      h(
        'div',
        { class: 'space-y-3' },
        componentProps.issues.map(issue =>
          h('article', { class: 'rounded-xl border border-cyan-800/70 bg-[#032a45]/85 p-4' }, [
            h('div', { class: 'flex items-start justify-between gap-3' }, [
              h('div', { class: 'space-y-2' }, [
                h('p', { class: 'font-medium text-cyan-50' }, issue.title),
                h('p', { class: 'font-mono text-xs text-cyan-100/75 break-all' }, issue.endpoint),
                h('p', { class: 'text-xs text-cyan-100/65' }, owaspLabel(issue))
              ]),
              h('div', { class: 'flex flex-col items-end gap-2' }, [
                h('span', { class: `rounded-full border px-3 py-1 text-xs ${severityClassName(issue.severity)}` }, issue.severity),
                h('span', { class: `rounded-full border px-3 py-1 text-xs ${confidenceClassName(issue.detectionConfidence)}` }, issue.detectionConfidence)
              ])
            ]),
            issue.openApiLocation
              ? h('p', { class: 'mt-3 font-mono text-xs text-cyan-100/65 break-all' }, issue.openApiLocation)
              : null
          ])
        )
      )
  }
})

const GroupList = defineComponent({
  name: 'ComparisonIssueGroupList',
  props: {
    groups: {
      type: Array as PropType<IssueGroup[]>,
      required: true
    },
    label: {
      type: String,
      required: true
    },
    accent: {
      type: String as PropType<'critical' | 'safe' | 'accent'>,
      required: true
    }
  },
  setup(componentProps) {
    const accentClassMap = {
      critical: 'border-critical/30 text-critical',
      safe: 'border-safe/30 text-safe',
      accent: 'border-accent/30 text-accent'
    } as const

    return () =>
      h(
        'div',
        { class: 'space-y-3' },
        componentProps.groups.map(group =>
          h('article', { class: 'rounded-xl border border-cyan-800/70 bg-[#032a45]/85 p-4' }, [
            h('div', { class: 'flex items-start justify-between gap-3' }, [
              h('div', { class: 'space-y-2' }, [
                h('p', { class: 'text-xs uppercase tracking-wide text-cyan-100/55' }, componentProps.label),
                h('p', { class: 'text-sm font-medium text-cyan-50 break-words' }, group.label),
                h('p', { class: 'text-xs text-cyan-100/65' }, `${group.count} finding(s) · ${group.ruleCodes.join(', ')}`)
              ]),
              h('div', { class: 'flex flex-col items-end gap-2' }, [
                h('span', { class: `rounded-full border px-3 py-1 text-xs ${accentClassMap[componentProps.accent]}` }, `${group.count}`),
                h('span', { class: `rounded-full border px-3 py-1 text-xs ${severityClassName(group.worstSeverity)}` }, group.worstSeverity)
              ])
            ]),
            componentProps.label !== 'Endpoint' && group.endpoints.length > 0
              ? h('p', { class: 'mt-3 font-mono text-xs text-cyan-100/65 break-all' }, group.endpoints.join(', '))
              : null
          ])
        )
      )
  }
})
</script>
