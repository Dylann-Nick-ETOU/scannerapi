<template>
  <section class="rounded-2xl border border-cyan-900/50 bg-night/40 p-8">
    <div class="mb-4 flex items-center justify-between gap-3">
      <h3 class="text-3xl font-semibold">Utilisateurs</h3>
      <button
        class="rounded-lg border border-cyan-700 px-4 py-2 text-sm text-cyan-100 hover:border-accent hover:text-accent"
        @click="$emit('refresh')"
      >
        Actualiser
      </button>
    </div>

    <p v-if="loading" class="text-cyan-200/80">Chargement...</p>
    <p v-else-if="error" class="text-sm text-critical">{{ error }}</p>
    <p v-else-if="items.length === 0" class="text-cyan-200/70">Aucun utilisateur trouvé.</p>

    <div v-else class="overflow-x-auto">
      <table class="w-full min-w-[1260px] text-left text-sm">
        <thead>
          <tr class="border-b border-cyan-900 text-cyan-100/70">
            <th class="px-3 py-3">Login</th>
            <th class="px-3 py-3">Rôle</th>
            <th class="px-3 py-3">Créé le</th>
            <th class="px-3 py-3">Dernière connexion</th>
            <th class="px-3 py-3">Statut</th>
            <th class="px-3 py-3">Scans</th>
            <th class="px-3 py-3 text-right">Actions</th>
          </tr>
        </thead>
        <tbody>
          <template v-for="item in items" :key="item.username">
            <tr class="border-b border-cyan-950">
              <td class="px-3 py-3">{{ item.username }}</td>
              <td class="px-3 py-3">{{ item.role }}</td>
              <td class="px-3 py-3">{{ formatDate(item.createdAt) }}</td>
              <td class="px-3 py-3">{{ item.lastLoginAt ? formatDate(item.lastLoginAt) : 'Jamais' }}</td>
              <td class="px-3 py-3">
                <span
                  class="rounded-full px-3 py-1 text-xs"
                  :class="item.isActive ? 'bg-[#12384e] text-safe' : 'bg-critical/15 text-critical'"
                >
                  {{ item.isActive ? 'Actif' : 'Désactivé' }}
                </span>
              </td>
              <td class="px-3 py-3">{{ item.scansCount }}</td>
              <td class="px-3 py-3 text-right">
                <div class="inline-flex gap-2">
                  <button
                    class="rounded border border-cyan-700 px-3 py-1 text-cyan-100 hover:border-accent hover:text-accent"
                    @click="toggleUser(item.username)"
                  >
                    {{ expandedUser === item.username ? 'Masquer ses scans' : 'Voir ses scans' }}
                  </button>
                  <button
                    class="rounded border border-critical/60 px-3 py-1 text-critical hover:bg-critical/10 disabled:cursor-not-allowed disabled:opacity-50"
                    :disabled="!item.isActive"
                    @click="$emit('deactivate', item.username)"
                  >
                    Désactiver
                  </button>
                </div>
              </td>
            </tr>

            <tr v-if="expandedUser === item.username" class="border-b border-cyan-950/80">
              <td colspan="7" class="px-3 py-4">
                <div v-if="item.scans.length === 0" class="text-cyan-200/70">Aucun scan enregistré.</div>
                <div v-else class="overflow-x-auto rounded-xl border border-cyan-800/70 bg-[#032a45]/70 p-4">
                  <table class="w-full min-w-[920px] text-left text-sm">
                    <thead>
                      <tr class="border-b border-cyan-900 text-cyan-100/70">
                        <th class="px-3 py-2">Cible</th>
                        <th class="px-3 py-2">Score</th>
                        <th class="px-3 py-2">Issues</th>
                        <th class="px-3 py-2">Statut</th>
                        <th class="px-3 py-2">Date</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr v-for="scan in item.scans" :key="scan.id" class="border-b border-cyan-950/80">
                        <td class="px-3 py-2">{{ scan.targetName }}</td>
                        <td class="px-3 py-2">{{ scan.score }}</td>
                        <td class="px-3 py-2">{{ scan.issuesCount }}</td>
                        <td class="px-3 py-2">{{ scan.status }}</td>
                        <td class="px-3 py-2">{{ formatDate(scan.createdAt) }}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </td>
            </tr>
          </template>
        </tbody>
      </table>
    </div>
  </section>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import type { AdminUserActivity } from '../types/scan'

defineProps<{
  items: AdminUserActivity[]
  loading: boolean
  error?: string
}>()

defineEmits<{
  refresh: []
  deactivate: [username: string]
}>()

const expandedUser = ref<string | null>(null)

function toggleUser(username: string) {
  expandedUser.value = expandedUser.value === username ? null : username
}

function formatDate(value: string): string {
  return new Date(value).toLocaleString('fr-FR')
}
</script>
