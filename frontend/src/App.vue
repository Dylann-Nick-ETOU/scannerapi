<template>
  <main class="min-h-screen bg-gradient-to-br from-night to-primary p-6 text-white md:p-10">
    <div class="mx-auto flex max-w-6xl flex-col gap-6">
      <HeroSection />
      <ScanForm :loading="loading" :error="error" @scan-url="handleScanUrl" @scan-file="handleScanFile" />

      <template v-if="report">
        <section class="grid gap-4 md:grid-cols-[1fr_2fr]">
          <ScoreCard :score="report.score" />
          <IssuesSummary :summary="report.summary" />
        </section>
        <IssuesTable :issues="report.issues" />
        <RecommendationCard :issues="report.issues" />
        <BeforeAfterSection />
      </template>
    </div>
  </main>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import BeforeAfterSection from './components/BeforeAfterSection.vue'
import HeroSection from './components/HeroSection.vue'
import IssuesSummary from './components/IssuesSummary.vue'
import IssuesTable from './components/IssuesTable.vue'
import RecommendationCard from './components/RecommendationCard.vue'
import ScanForm from './components/ScanForm.vue'
import ScoreCard from './components/ScoreCard.vue'
import { scanFromFile, scanFromUrl } from './services/scanApi'
import type { ScanReport } from './types/scan'

const loading = ref(false)
const error = ref('')
const report = ref<ScanReport | null>(null)

async function handleScanUrl(url: string) {
  loading.value = true
  error.value = ''

  try {
    report.value = await scanFromUrl({ openApiUrl: url })
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
  } catch (err: unknown) {
    report.value = null
    error.value = 'Impossible de scanner ce fichier. Vérifiez le format OpenAPI JSON/YAML.'
    console.error(err)
  } finally {
    loading.value = false
  }
}
</script>
