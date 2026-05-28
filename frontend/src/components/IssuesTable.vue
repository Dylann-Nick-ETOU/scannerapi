<template>
  <section class="rounded-2xl border border-cyan-900/50 bg-night/40 p-8">
    <h3 class="text-3xl font-semibold">Liste des failles détectées</h3>
    <div class="mt-6 overflow-x-auto">
      <table class="w-full min-w-[780px] text-left text-sm">
        <thead>
          <tr class="border-b border-cyan-900 text-cyan-100/70">
            <th class="py-3">Sévérité</th>
            <th class="py-3">Code règle</th>
            <th class="py-3">Endpoint</th>
            <th class="py-3">Problème détecté</th>
            <th class="py-3">Recommandation</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="issue in issues" :key="`${issue.ruleCode}-${issue.endpoint}`" class="border-b border-cyan-950">
            <td class="py-3">{{ issue.severity }}</td>
            <td class="py-3"><span class="rounded bg-primary px-2 py-1 text-accent">{{ issue.ruleCode }}</span></td>
            <td class="py-3">{{ issue.endpoint }}</td>
            <td class="py-3">{{ issue.title }}</td>
            <td class="py-3">{{ issue.recommendation }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
</template>

<script setup lang="ts">
import type { SecurityIssue } from '../types/scan'

defineProps<{ issues: SecurityIssue[] }>()
</script>
