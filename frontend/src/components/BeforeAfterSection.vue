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
            <span class="rounded-full border border-critical/40 px-3 py-1 text-sm text-critical">{{ comparison.summary.newIssuesCount }}</span>
          </div>
          <IssueList :issues="comparison.newIssues" empty-text="Aucune nouvelle faille détectée." />
        </article>

        <article class="rounded-2xl border border-safe/40 bg-[#042f4b] p-6">
          <div class="mb-4 flex items-center justify-between gap-3">
            <h4 class="text-xl font-semibold text-safe">Failles corrigées</h4>
            <span class="rounded-full border border-safe/40 px-3 py-1 text-sm text-safe">{{ comparison.summary.resolvedIssuesCount }}</span>
          </div>
          <IssueList :issues="comparison.resolvedIssues" empty-text="Aucune faille résolue dans cet intervalle." />
        </article>

        <article class="rounded-2xl border border-accent/40 bg-[#042f4b] p-6">
          <div class="mb-4 flex items-center justify-between gap-3">
            <h4 class="text-xl font-semibold text-accent">Toujours présentes</h4>
            <span class="rounded-full border border-accent/40 px-3 py-1 text-sm text-accent">{{ comparison.summary.unchangedIssuesCount }}</span>
          </div>
          <IssueList :issues="comparison.unchangedIssues" empty-text="Aucune faille persistante." />
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
import type { ScanComparison, ScanHistoryItem, SecurityIssue } from '../types/scan'
import { owaspLabel, sortIssues } from '../utils/issuePresentation'

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

watch(() => props.currentScanId, value => {
  selectedCurrentId.value = value ?? ''
})

watch(() => props.baselineScanId, value => {
  selectedBaselineId.value = value ?? ''
})

const canCompare = computed(() =>
  Boolean(selectedCurrentId.value && selectedBaselineId.value && selectedCurrentId.value !== selectedBaselineId.value)
)

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

const IssueList = defineComponent({
  name: 'ComparisonIssueList',
  props: {
    issues: {
      type: Array as PropType<SecurityIssue[]>,
      required: true
    },
    emptyText: {
      type: String,
      required: true
    }
  },
  setup(componentProps) {
    const orderedIssues = computed(() => sortIssues(componentProps.issues))

    function severityClass(severity: SecurityIssue['severity']): string {
      if (severity === 'Critical') return 'border-critical/50 text-critical bg-critical/10'
      if (severity === 'High') return 'border-warning/50 text-warning bg-warning/10'
      if (severity === 'Medium') return 'border-accent/50 text-accent bg-accent/10'
      return 'border-cyan-500/50 text-cyan-200 bg-cyan-500/10'
    }

    return () => {
      if (orderedIssues.value.length === 0) {
        return h('p', { class: 'text-sm text-cyan-100/70' }, componentProps.emptyText)
      }

      return h(
        'div',
        { class: 'space-y-3' },
        orderedIssues.value.map(issue =>
          h('article', { class: 'rounded-xl border border-cyan-800/70 bg-[#032a45]/85 p-4' }, [
            h('div', { class: 'flex items-start justify-between gap-3' }, [
              h('div', { class: 'space-y-2' }, [
                h('p', { class: 'font-medium text-cyan-50' }, issue.title),
                h('p', { class: 'font-mono text-xs text-cyan-100/75 break-all' }, issue.endpoint),
                h('p', { class: 'text-xs text-cyan-100/65' }, owaspLabel(issue))
              ]),
              h('span', { class: `rounded-full border px-3 py-1 text-xs ${severityClass(issue.severity)}` }, issue.severity)
            ]),
            issue.openApiLocation
              ? h('p', { class: 'mt-3 font-mono text-xs text-cyan-100/65 break-all' }, issue.openApiLocation)
              : null
          ])
        )
      )
    }
  }
})
</script>
