<template>
  <section class="rounded-2xl border border-cyan-900/50 bg-night/40 p-8">
    <div class="mb-4 flex items-center justify-between gap-3">
      <h3 class="text-3xl font-semibold">Historique des scans</h3>
      <button
        class="rounded-lg border border-cyan-700 px-4 py-2 text-sm text-cyan-100 hover:border-accent hover:text-accent"
        @click="$emit('refresh')"
      >
        Actualiser
      </button>
    </div>

    <p v-if="loading" class="text-cyan-200/80">Chargement...</p>
    <p v-else-if="error" class="text-sm text-critical">{{ error }}</p>
    <p v-else-if="items.length === 0" class="text-cyan-200/70">Aucun scan enregistré.</p>

    <div v-else class="overflow-x-auto">
      <table class="w-full min-w-[1080px] text-left text-sm">
        <thead>
          <tr class="border-b border-cyan-900 text-cyan-100/70">
            <th class="px-3 py-3">Cible</th>
            <th class="px-3 py-3">Score</th>
            <th class="px-3 py-3">Issues</th>
            <th class="px-3 py-3">Statut</th>
            <th class="px-3 py-3">Date</th>
            <th class="px-3 py-3 text-right">Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="item in items" :key="item.id" class="border-b border-cyan-950">
            <td class="px-3 py-3">{{ item.targetName }}</td>
            <td class="px-3 py-3">{{ item.score }}</td>
            <td class="px-3 py-3">{{ item.issuesCount }}</td>
            <td class="px-3 py-3">{{ item.status }}</td>
            <td class="px-3 py-3">{{ formatDate(item.createdAt) }}</td>
            <td class="px-3 py-3 text-right">
              <div class="inline-flex gap-2">
                <button
                  class="rounded border border-cyan-700 px-3 py-1 text-cyan-100 hover:border-accent hover:text-accent"
                  @click="$emit('view', item.id)"
                >
                  Voir
                </button>
                <button
                  class="rounded border border-cyan-700 px-3 py-1 text-cyan-100 hover:border-accent hover:text-accent"
                  @click="$emit('export', item.id)"
                >
                  Exporter JSON
                </button>
                <button
                  class="rounded border border-cyan-700 px-3 py-1 text-cyan-100 hover:border-safe hover:text-safe"
                  @click="$emit('compare', item.id)"
                >
                  Comparer
                </button>
                <button
                  class="rounded border border-critical/60 px-3 py-1 text-critical hover:bg-critical/10"
                  @click="$emit('remove', item.id)"
                >
                  Supprimer
                </button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
</template>

<script setup lang="ts">
import type { ScanHistoryItem } from '../types/scan'

defineProps<{
  items: ScanHistoryItem[]
  loading: boolean
  error?: string
}>()

defineEmits<{
  refresh: []
  view: [id: string]
  export: [id: string]
  compare: [id: string]
  remove: [id: string]
}>()

function formatDate(value: string): string {
  return new Date(value).toLocaleString('fr-FR')
}
</script>
