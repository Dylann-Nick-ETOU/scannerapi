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

    <div v-else class="space-y-5">
      <div class="grid gap-3 rounded-2xl border border-cyan-800/70 bg-[#04314e]/70 p-4 md:grid-cols-3">
        <label class="flex flex-col gap-2 text-sm text-cyan-100/85">
          <span>Recherche</span>
          <input
            v-model.trim="searchQuery"
            type="text"
            placeholder="login, rôle, statut..."
            class="h-11 rounded-xl border border-cyan-800 bg-[#032a45] px-4 text-cyan-50 outline-none transition placeholder:text-cyan-200/45 focus:border-accent"
          >
        </label>

        <label class="flex flex-col gap-2 text-sm text-cyan-100/85">
          <span>Rôle</span>
          <select
            v-model="roleFilter"
            class="h-11 rounded-xl border border-cyan-800 bg-[#032a45] px-4 text-cyan-50 outline-none transition focus:border-accent"
          >
            <option value="">Tous</option>
            <option v-for="role in roles" :key="role" :value="role">{{ role }}</option>
          </select>
        </label>

        <label class="flex flex-col gap-2 text-sm text-cyan-100/85">
          <span>Statut</span>
          <select
            v-model="statusFilter"
            class="h-11 rounded-xl border border-cyan-800 bg-[#032a45] px-4 text-cyan-50 outline-none transition focus:border-accent"
          >
            <option value="">Tous</option>
            <option value="active">Actif</option>
            <option value="inactive">Désactivé</option>
          </select>
        </label>
      </div>

      <div class="flex flex-col gap-3 text-sm text-cyan-100/80 lg:flex-row lg:items-center lg:justify-between">
        <p>{{ filteredItems.length }} utilisateur(s) affiché(s) sur {{ items.length }}</p>
        <button
          class="inline-flex h-10 items-center justify-center rounded-xl border border-cyan-700 px-4 text-cyan-100 transition hover:border-accent hover:text-accent disabled:cursor-not-allowed disabled:opacity-50"
          :disabled="!hasActiveFilters"
          @click="resetFilters"
        >
          Réinitialiser les filtres
        </button>
      </div>

      <div class="overflow-x-auto">
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
            <template v-for="item in filteredItems" :key="item.username">
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
                      v-if="item.isActive"
                      class="rounded border border-critical/60 px-3 py-1 text-critical hover:bg-critical/10"
                      @click="$emit('deactivate', item.username)"
                    >
                      Désactiver
                    </button>
                    <button
                      v-else
                      class="rounded border border-safe/60 px-3 py-1 text-safe hover:bg-safe/10"
                      @click="$emit('reactivate', item.username)"
                    >
                      Réactiver
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
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import type { AdminUserActivity } from '../types/scan'

const props = defineProps<{
  items: AdminUserActivity[]
  loading: boolean
  error?: string
}>()

defineEmits<{
  refresh: []
  deactivate: [username: string]
  reactivate: [username: string]
}>()

const expandedUser = ref<string | null>(null)
const searchQuery = ref('')
const roleFilter = ref('')
const statusFilter = ref<'active' | 'inactive' | ''>('')

const roles = computed(() => [...new Set(props.items.map(item => item.role))].sort((left, right) => left.localeCompare(right, 'fr')))

const filteredItems = computed(() => {
  const query = searchQuery.value.trim().toLowerCase()

  return props.items.filter(item => {
    if (roleFilter.value && item.role !== roleFilter.value) {
      return false
    }

    if (statusFilter.value === 'active' && !item.isActive) {
      return false
    }

    if (statusFilter.value === 'inactive' && item.isActive) {
      return false
    }

    if (!query) {
      return true
    }

    const haystack = [
      item.username,
      item.role,
      item.isActive ? 'actif' : 'désactivé'
    ]
      .join(' ')
      .toLowerCase()

    return haystack.includes(query)
  })
})

const hasActiveFilters = computed(() => Boolean(searchQuery.value || roleFilter.value || statusFilter.value))

function toggleUser(username: string) {
  expandedUser.value = expandedUser.value === username ? null : username
}

function resetFilters() {
  searchQuery.value = ''
  roleFilter.value = ''
  statusFilter.value = ''
}

function formatDate(value: string): string {
  return new Date(value).toLocaleString('fr-FR')
}
</script>
