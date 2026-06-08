<template>
  <main class="grid-bg min-h-screen text-white">
    <header class="border-b border-cyan-900/70 bg-[#00213a]/70 backdrop-blur-sm">
      <div class="mx-auto flex max-w-[1280px] items-center justify-between px-6 py-3 md:px-10">
        <button class="flex items-center gap-3" @click="go('accueil')">
          <div class="flex h-9 w-9 items-center justify-center rounded-xl bg-[#12384e] text-accent">🛡</div>
          <span class="text-2xl font-medium text-cyan-50">API Security Scanner</span>
        </button>

        <nav v-if="authState" class="hidden items-center gap-2 rounded-2xl border border-cyan-800/60 bg-[#03243c]/70 p-1 md:flex">
          <button
            v-for="item in navItems"
            :key="item.key"
            class="rounded-xl px-4 py-2 text-sm font-medium transition"
            :class="page === item.key ? 'bg-accent text-night' : 'text-cyan-100/90 hover:bg-cyan-900/40'"
            @click="go(item.key)"
          >
            {{ item.label }}
          </button>
        </nav>

        <div class="flex items-center gap-3">
          <div class="hidden text-right text-xs text-cyan-200/80 lg:block">
            <p>{{ authIdentityLabel }}</p>
          </div>

          <button
            v-if="!authState"
            class="rounded-xl border border-cyan-700 px-4 py-2 text-sm text-cyan-100 hover:border-accent hover:text-accent"
            @click="go('connexion')"
          >
            Connexion
          </button>

          <button
            v-if="authState"
            class="rounded-xl border border-cyan-700 px-4 py-2 text-sm text-cyan-100 hover:border-critical hover:text-critical"
            @click="handleLogout"
          >
            Déconnexion
          </button>

          <button
            v-if="authState"
            class="rounded-2xl bg-accent px-6 py-2 text-sm font-semibold text-night shadow-[0_0_14px_rgba(255,214,51,0.3)] hover:brightness-110"
            @click="go('scanner')"
          >
            Lancer un scan
          </button>
        </div>
      </div>

      <div v-if="authState" class="mx-auto max-w-[1280px] px-6 pb-3 md:hidden md:px-10">
        <nav class="flex gap-2 overflow-x-auto rounded-2xl border border-cyan-800/60 bg-[#03243c]/70 p-1">
          <button
            v-for="item in navItems"
            :key="item.key"
            class="shrink-0 rounded-xl px-4 py-2 text-sm font-medium transition"
            :class="page === item.key ? 'bg-accent text-night' : 'text-cyan-100/90 hover:bg-cyan-900/40'"
            @click="go(item.key)"
          >
            {{ item.label }}
          </button>
        </nav>
      </div>
    </header>

    <div class="mx-auto flex w-full flex-col gap-10 px-6 pb-14 pt-16 md:px-10 md:pt-20" :class="contentWidthClass">
      <Transition name="fade-slide" mode="out-in">
        <section v-if="page === 'accueil'" key="accueil">
          <HeroSection @start="go('scanner')" @demo="go('avant-apres')" />
        </section>

        <section v-else-if="page === 'connexion'" key="connexion">
          <AuthPage :loading="authLoading" :error="error" @login="handleLogin" @register="handleRegister" />
        </section>

        <section v-else-if="page === 'scanner'" key="scanner">
          <ScanForm :loading="loading" :error="error" @scan-url="handleScanUrl" @scan-file="handleScanFile" />
        </section>

        <section v-else-if="page === 'rapport'" key="rapport" class="space-y-6">
          <template v-if="report">
            <div class="flex items-center justify-end">
              <button
                class="rounded-lg border border-cyan-700 px-4 py-2 text-sm text-cyan-100 hover:border-accent hover:text-accent"
                @click="handleExportScan(report.scanId)"
              >
                Exporter le rapport JSON
              </button>
            </div>
            <div class="grid gap-6 md:grid-cols-[1fr_1.45fr] md:items-start">
              <ScoreCard :score="report.score" />
              <IssuesSummary :summary="report.summary" />
            </div>
            <IssuesTable
              :scan-id="report.scanId"
              :issues="report.issues"
              @issue-updated="handleIssueUpdated"
              @action-error="handleIssueActionError"
            />
          </template>
          <p v-else class="text-cyan-100/80">Aucun rapport chargé. Lancez un scan depuis la page Scanner.</p>
        </section>

        <section v-else-if="page === 'admin'" key="admin">
          <AdminUsersSection
            :items="adminUsers"
            :loading="adminLoading"
            :error="adminError"
            @refresh="loadAdminUsers"
            @deactivate="handleDeactivateUser"
          />
        </section>

        <section v-else-if="page === 'recommandations'" key="recommandations">
          <RecommendationCard v-if="report" :issues="report.issues" />
          <p v-else class="text-cyan-100/80">Aucun rapport chargé. Lancez un scan depuis la page Scanner.</p>
        </section>

        <section v-else-if="page === 'avant-apres'" key="avant-apres">
          <BeforeAfterSection />
        </section>

        <section v-else key="historique">
          <ScanHistorySection
            :items="history"
            :loading="historyLoading"
            :error="historyError"
            @refresh="loadHistory"
            @view="handleViewScan"
            @export="handleExportScan"
            @remove="handleDeleteScan"
          />
        </section>
      </Transition>
    </div>
  </main>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import axios from 'axios'
import AdminUsersSection from './components/AdminUsersSection.vue'
import AuthPage from './components/AuthPage.vue'
import BeforeAfterSection from './components/BeforeAfterSection.vue'
import HeroSection from './components/HeroSection.vue'
import IssuesSummary from './components/IssuesSummary.vue'
import IssuesTable from './components/IssuesTable.vue'
import RecommendationCard from './components/RecommendationCard.vue'
import ScanForm from './components/ScanForm.vue'
import ScanHistorySection from './components/ScanHistorySection.vue'
import ScoreCard from './components/ScoreCard.vue'
import { clearAccessToken, deactivateUser, deleteScan, exportScanJson, getAccessToken, getAdminUsers, getScanById, getScans, login, readStoredAuthState, register, scanFromFile, scanFromUrl } from './services/scanApi'
import type { AdminUserActivity, AuthResponse, RegisterRequest, ScanHistoryItem, ScanReport, SecurityIssue } from './types/scan'

type Page = 'accueil' | 'connexion' | 'scanner' | 'rapport' | 'historique' | 'admin' | 'avant-apres' | 'recommandations'

const page = ref<Page>(getAccessToken() ? 'accueil' : 'connexion')
const navItems = computed<Array<{ key: Page; label: string }>>(() => {
  const items: Array<{ key: Page; label: string }> = [
    { key: 'scanner', label: 'Scanner' },
    { key: 'historique', label: 'Historique' },
    { key: 'rapport', label: 'Rapport' }
  ]

  if (authState.value?.role === 'Admin') {
    items.push({ key: 'admin', label: 'Utilisateurs' })
  }

  return items
})

const loading = ref(false)
const error = ref('')
const report = ref<ScanReport | null>(null)
const history = ref<ScanHistoryItem[]>([])
const historyLoading = ref(false)
const historyError = ref('')
const adminUsers = ref<AdminUserActivity[]>([])
const adminLoading = ref(false)
const adminError = ref('')
const authLoading = ref(false)
const authState = ref<AuthResponse | null>(readStoredAuthState())
const authIdentityLabel = computed(() =>
  authState.value ? `${authState.value.username} (${authState.value.role})` : 'Non connecté'
)
const contentWidthClass = computed(() =>
  page.value === 'rapport' || page.value === 'admin' || page.value === 'recommandations'
    ? 'max-w-[1460px]'
    : 'max-w-[1280px]'
)

function go(target: Page) {
  if (!authState.value && target !== 'connexion' && target !== 'accueil') {
    error.value = 'Connectez-vous pour accéder à cette section.'
    page.value = 'connexion'
    return
  }

  if (target === 'admin' && authState.value?.role !== 'Admin') {
    error.value = 'Accès réservé aux administrateurs.'
    return
  }

  page.value = target
}

async function handleLogin(payload: { username: string; password: string }) {
  authLoading.value = true
  error.value = ''
  historyError.value = ''
  try {
    authState.value = await login(payload)
    await loadHistory()
    if (authState.value.role === 'Admin') {
      await loadAdminUsers()
    }
    page.value = 'historique'
  } catch (err: unknown) {
    authState.value = null
    error.value = extractApiError(err, 'Impossible de se connecter.')
  } finally {
    authLoading.value = false
  }
}

async function handleRegister(payload: RegisterRequest) {
  authLoading.value = true
  error.value = ''
  historyError.value = ''
  try {
    authState.value = await register(payload)
    await loadHistory()
    page.value = 'historique'
  } catch (err: unknown) {
    authState.value = null
    error.value = extractApiError(err, 'Impossible de créer le compte.')
  } finally {
    authLoading.value = false
  }
}

function handleLogout() {
  clearAccessToken()
  authState.value = null
  report.value = null
  history.value = []
  adminUsers.value = []
  error.value = ''
  historyError.value = ''
  adminError.value = ''
  page.value = 'connexion'
}

async function loadHistory() {
  historyLoading.value = true
  historyError.value = ''
  try {
    history.value = await getScans()
  } catch (err: unknown) {
    historyError.value = extractApiError(err, 'Impossible de charger l\'historique des scans.')
    console.error(err)
  } finally {
    historyLoading.value = false
  }
}

async function loadAdminUsers() {
  if (authState.value?.role !== 'Admin') {
    return
  }

  adminLoading.value = true
  adminError.value = ''
  try {
    adminUsers.value = await getAdminUsers()
  } catch (err: unknown) {
    adminError.value = extractApiError(err, 'Impossible de charger les utilisateurs.')
    console.error(err)
  } finally {
    adminLoading.value = false
  }
}

async function handleDeactivateUser(username: string) {
  try {
    await deactivateUser(username)
    await loadAdminUsers()
  } catch (err: unknown) {
    adminError.value = extractApiError(err, 'Impossible de désactiver cet utilisateur.')
    console.error(err)
  }
}

async function handleScanUrl(url: string) {
  loading.value = true
  error.value = ''
  try {
    report.value = await scanFromUrl({ openApiUrl: url })
    await loadHistory()
    page.value = 'rapport'
  } catch (err: unknown) {
    report.value = null
    error.value = extractApiError(err, 'Impossible de scanner cette URL. Vérifiez que le document OpenAPI est accessible.')
    console.error(err)
  } finally {
    loading.value = false
  }
}

async function handleScanFile(file: File) {
  loading.value = true
  error.value = ''
  try {
    report.value = await scanFromFile(file)
    await loadHistory()
    page.value = 'rapport'
  } catch (err: unknown) {
    report.value = null
    error.value = extractApiError(err, 'Impossible de scanner ce fichier. Vérifiez le format OpenAPI JSON/YAML.')
    console.error(err)
  } finally {
    loading.value = false
  }
}

async function handleViewScan(id: string) {
  loading.value = true
  error.value = ''
  try {
    report.value = await getScanById(id)
    page.value = 'rapport'
  } catch (err) {
    error.value = extractApiError(err, 'Impossible de charger ce rapport.')
    console.error(err)
  } finally {
    loading.value = false
  }
}

async function handleExportScan(id: string) {
  try {
    const blob = await exportScanJson(id)
    const url = window.URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `scan-${id}.json`
    document.body.appendChild(link)
    link.click()
    link.remove()
    window.URL.revokeObjectURL(url)
  } catch (err) {
    error.value = 'Impossible d\'exporter ce scan.'
    console.error(err)
  }
}

async function handleDeleteScan(id: string) {
  try {
    await deleteScan(id)
    if (report.value?.scanId === id) report.value = null
    await loadHistory()
  } catch (err) {
    error.value = extractApiError(err, 'Impossible de supprimer ce scan.')
    console.error(err)
  }
}

function handleIssueUpdated(updatedIssue: SecurityIssue) {
  if (!report.value) {
    return
  }

  report.value = {
    ...report.value,
    issues: report.value.issues.map(issue => issue.id === updatedIssue.id ? updatedIssue : issue)
  }
}

function handleIssueActionError(message: string) {
  error.value = message
}

function extractApiError(err: unknown, fallback: string): string {
  if (axios.isAxiosError(err)) {
    const status = err.response?.status
    const message = err.response?.data?.message

    if (status === 401) {
      return 'Session non authentifiée. Reconnectez-vous avant de relancer cette action.'
    }

    if (status === 403) {
      return 'Accès refusé. Votre compte ne possède pas les droits nécessaires pour cette action.'
    }

    if (status === 404) {
      return 'Ressource introuvable ou non accessible avec votre compte.'
    }

    if (status === 429) {
      return 'Trop de requêtes envoyées. Attendez une minute avant de recommencer.'
    }

    if (typeof message === 'string' && message.trim().length > 0) {
      return message
    }
  }

  return fallback
}

onMounted(async () => {
  if (getAccessToken()) {
    await loadHistory()
    if (authState.value?.role === 'Admin') {
      await loadAdminUsers()
    }
  }
})
</script>

<style scoped>
.fade-slide-enter-active,
.fade-slide-leave-active {
  transition: all 0.2s ease;
}
.fade-slide-enter-from,
.fade-slide-leave-to {
  opacity: 0;
  transform: translateY(8px);
}
</style>
