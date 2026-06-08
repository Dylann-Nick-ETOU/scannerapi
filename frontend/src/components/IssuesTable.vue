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

    <div v-if="viewMode === 'detail'" class="mt-6 overflow-x-auto rounded-2xl border border-cyan-800/70">
      <table class="w-full min-w-[1240px] text-left text-lg">
        <thead class="bg-[#04314e]">
          <tr class="border-b border-cyan-800 text-cyan-100">
            <th class="px-4 py-4">Sévérité</th>
            <th class="px-4 py-4">Confiance</th>
            <th class="px-4 py-4">Code règle</th>
            <th class="px-4 py-4">OWASP</th>
            <th class="px-4 py-4">Endpoint</th>
            <th class="px-4 py-4">Chemin spec</th>
            <th class="px-4 py-4">Problème détecté</th>
            <th class="px-4 py-4">Recommandation</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="issue in sortedIssues" :key="`${issue.ruleCode}-${issue.endpoint}-${issue.title}`" class="border-b border-cyan-900/70">
            <td class="px-4 py-4"><span class="rounded-full border px-3 py-1 text-sm" :class="severityClass(issue.severity)">{{ issue.severity }}</span></td>
            <td class="px-4 py-4"><span class="rounded-full border px-3 py-1 text-sm" :class="confidenceClass(issue.detectionConfidence)">{{ issue.detectionConfidence }}</span></td>
            <td class="px-4 py-4"><span class="rounded bg-[#355d38] px-3 py-1 font-mono text-accent">{{ issue.ruleCode }}</span></td>
            <td class="px-4 py-4 text-cyan-100/90">{{ owaspLabel(issue) }}</td>
            <td class="px-4 py-4 font-mono text-cyan-100">{{ issue.endpoint }}</td>
            <td class="px-4 py-4 font-mono text-sm text-cyan-100/80 break-all">{{ issue.openApiLocation || '-' }}</td>
            <td class="px-4 py-4 text-cyan-100/90">
              <p>{{ issue.title }}</p>
              <pre v-if="issue.openApiExcerpt" class="mt-3 overflow-x-auto rounded bg-[#082e44] p-3 text-xs text-cyan-100/85 whitespace-pre-wrap break-words">{{ issue.openApiExcerpt }}</pre>
            </td>
            <td class="px-4 py-4 text-cyan-100/90">{{ issue.recommendation }}</td>
          </tr>
        </tbody>
      </table>
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
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import type { SecurityIssue } from '../types/scan'
import { groupIssuesByEndpoint, groupIssuesByOwasp, owaspLabel, sortIssues } from '../utils/issuePresentation'

const props = defineProps<{ issues: SecurityIssue[] }>()
const viewMode = ref<'detail' | 'owasp' | 'endpoint'>('detail')
const viewModes = [
  { key: 'detail', label: 'Détail' },
  { key: 'owasp', label: 'Par OWASP' },
  { key: 'endpoint', label: 'Par endpoint' }
] as const

const sortedIssues = computed(() => sortIssues(props.issues))
const owaspGroups = computed(() => groupIssuesByOwasp(props.issues))
const endpointGroups = computed(() => groupIssuesByEndpoint(props.issues))

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
</script>
