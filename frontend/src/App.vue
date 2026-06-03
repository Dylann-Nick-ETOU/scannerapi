<template>
  <main class="grid-bg min-h-screen text-white">
    <header class="border-b border-cyan-900/70 bg-[#00213a]/70 backdrop-blur-sm">
      <div class="mx-auto flex max-w-[1280px] items-center justify-between px-6 py-3 md:px-10">
        <button class="flex items-center gap-3" @click="go('accueil')">
          <div class="flex h-9 w-9 items-center justify-center rounded-xl bg-[#12384e] text-accent">🛡</div>
          <span class="text-2xl font-medium text-cyan-50">API Security Scanner</span>
        </button>

        <nav class="hidden items-center gap-2 rounded-2xl border border-cyan-800/60 bg-[#03243c]/70 p-1 md:flex">
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

        <button
          class="rounded-2xl bg-accent px-6 py-2 text-sm font-semibold text-night shadow-[0_0_14px_rgba(255,214,51,0.3)] hover:brightness-110"
          @click="go('scanner')"
        >
          Lancer un scan
        </button>
      </div>
    </header>

    <div class="mx-auto flex w-full max-w-[1280px] flex-col gap-10 px-6 pb-14 pt-16 md:px-10 md:pt-20">
      <Transition name="fade-slide" mode="out-in">
        <section v-if="page === 'accueil'" key="accueil">
          <HeroSection @start="go('scanner')" @demo="go('avant-apres')" />
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
            <IssuesTable :issues="report.issues" />
          </template>
          <p v-else class="text-cyan-100/80">Aucun rapport chargé. Lancez un scan depuis la page Scanner.</p>
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
import { onMounted, ref } from 'vue'
import BeforeAfterSection from './components/BeforeAfterSection.vue'
import HeroSection from './components/HeroSection.vue'
import IssuesSummary from './components/IssuesSummary.vue'
import IssuesTable from './components/IssuesTable.vue'
import RecommendationCard from './components/RecommendationCard.vue'
import ScanForm from './components/ScanForm.vue'
import ScanHistorySection from './components/ScanHistorySection.vue'
import ScoreCard from './components/ScoreCard.vue'
import { deleteScan, exportScanJson, getScanById, getScans, scanFromFile, scanFromUrl } from './services/scanApi'
import type { ScanHistoryItem, ScanReport } from './types/scan'

type Page = 'accueil' | 'scanner' | 'rapport' | 'historique' | 'avant-apres' | 'recommandations'

const page = ref<Page>('accueil')
const navItems: Array<{ key: Page; label: string }> = [
  { key: 'accueil', label: 'Accueil' },
  { key: 'scanner', label: 'Scanner' },
  { key: 'rapport', label: 'Rapport' },
  { key: 'historique', label: 'Historique' },
  { key: 'avant-apres', label: 'Avant / Après' },
  { key: 'recommandations', label: 'Recommandations' }
]

const loading = ref(false)
const error = ref('')
const report = ref<ScanReport | null>(null)
const history = ref<ScanHistoryItem[]>([])
const historyLoading = ref(false)

function go(target: Page) {
  page.value = target
}

async function loadHistory() {
  historyLoading.value = true
  try {
    history.value = await getScans()
  } catch (err) {
    console.error(err)
  } finally {
    historyLoading.value = false
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
    error.value = 'Impossible de scanner cette URL. Vérifiez que le document OpenAPI est accessible.'
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
    error.value = 'Impossible de scanner ce fichier. Vérifiez le format OpenAPI JSON/YAML.'
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
    error.value = 'Impossible de charger ce rapport.'
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
    error.value = 'Impossible de supprimer ce scan.'
    console.error(err)
  }
}

onMounted(loadHistory)
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
