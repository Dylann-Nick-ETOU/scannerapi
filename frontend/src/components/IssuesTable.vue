<template>
  <section class="rounded-2xl border border-cyan-800/70 bg-[#032a45]/85 p-8">
    <h3 class="text-2xl font-semibold">Liste des failles détectées</h3>
    <p class="mt-2 text-cyan-100/80">Détails des vulnérabilités identifiées</p>
    <div class="mt-6 overflow-x-auto rounded-2xl border border-cyan-800/70">
      <table class="w-full min-w-[940px] text-left text-lg">
        <thead class="bg-[#04314e]">
          <tr class="border-b border-cyan-800 text-cyan-100">
            <th class="px-4 py-4">Sévérité</th>
            <th class="px-4 py-4">Code règle</th>
            <th class="px-4 py-4">Endpoint</th>
            <th class="px-4 py-4">Problème détecté</th>
            <th class="px-4 py-4">Recommandation</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="issue in issues" :key="`${issue.ruleCode}-${issue.endpoint}-${issue.title}`" class="border-b border-cyan-900/70">
            <td class="px-4 py-4"><span class="rounded-full border px-3 py-1 text-sm" :class="severityClass(issue.severity)">{{ issue.severity }}</span></td>
            <td class="px-4 py-4"><span class="rounded bg-[#355d38] px-3 py-1 font-mono text-accent">{{ issue.ruleCode }}</span></td>
            <td class="px-4 py-4 font-mono text-cyan-100">{{ issue.endpoint }}</td>
            <td class="px-4 py-4 text-cyan-100/90">{{ issue.title }}</td>
            <td class="px-4 py-4 text-cyan-100/90">{{ issue.recommendation }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
</template>

<script setup lang="ts">
import type { SecurityIssue } from '../types/scan'
defineProps<{ issues: SecurityIssue[] }>()

function severityClass(severity: SecurityIssue['severity']): string {
  if (severity === 'Critical') return 'border-critical/50 text-critical bg-critical/10'
  if (severity === 'High') return 'border-warning/50 text-warning bg-warning/10'
  if (severity === 'Medium') return 'border-accent/50 text-accent bg-accent/10'
  return 'border-cyan-500/50 text-cyan-200 bg-cyan-500/10'
}
</script>
