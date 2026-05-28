<template>
  <main class="min-h-screen bg-gradient-to-br from-night to-primary p-6 text-white md:p-10">
    <div class="mx-auto flex max-w-6xl flex-col gap-6">
      <HeroSection />
      <ScanForm />
      <ScoreCard />
      <IssuesSummary />
      <IssuesTable />
      <RecommendationCard />
      <BeforeAfterSection />
    </div>
  </main>
</template>

<script setup lang="ts">
import BeforeAfterSection from './components/BeforeAfterSection.vue'
import HeroSection from './components/HeroSection.vue'
import IssuesSummary from './components/IssuesSummary.vue'
import IssuesTable from './components/IssuesTable.vue'
import RecommendationCard from './components/RecommendationCard.vue'
import ScanForm from './components/ScanForm.vue'
import ScoreCard from './components/ScoreCard.vue'
</script>
