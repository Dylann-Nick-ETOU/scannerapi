<template>
  <section>
    <h2 class="text-2xl font-semibold">Résultat du scan</h2>
    <p class="mt-2 text-cyan-100/80">Analyse de sécurité complétée</p>

    <div class="mt-6 grid gap-4 md:grid-cols-2">
      <article class="rounded-2xl border border-critical/45 bg-[#032a45]/85 p-6">
        <div class="flex items-center justify-between"><span class="text-2xl text-critical">🛡</span><span class="text-2xl text-critical">{{ summary.critical }}</span></div>
        <p class="mt-4 text-2xl">Critical</p><p class="text-cyan-100/75">Nécessite action immédiate</p>
      </article>
      <article class="rounded-2xl border border-warning/45 bg-[#032a45]/85 p-6">
        <div class="flex items-center justify-between"><span class="text-2xl text-warning">⚠</span><span class="text-2xl text-warning">{{ summary.high }}</span></div>
        <p class="mt-4 text-2xl">High</p><p class="text-cyan-100/75">Correction prioritaire</p>
      </article>
      <article class="rounded-2xl border border-accent/45 bg-[#032a45]/85 p-6">
        <div class="flex items-center justify-between"><span class="text-2xl text-accent">❕</span><span class="text-2xl text-accent">{{ summary.medium }}</span></div>
        <p class="mt-4 text-2xl">Medium</p><p class="text-cyan-100/75">À corriger prochainement</p>
      </article>
      <article class="rounded-2xl border border-cyan-500/35 bg-[#032a45]/85 p-6">
        <div class="flex items-center justify-between"><span class="text-2xl text-cyan-300">ⓘ</span><span class="text-2xl text-cyan-200">{{ summary.low }}</span></div>
        <p class="mt-4 text-2xl">Low</p><p class="text-cyan-100/75">Bonne pratique</p>
      </article>
    </div>
  </section>
</template>

<script setup lang="ts">
import type { ScanSummary } from '../types/scan'
defineProps<{ summary: ScanSummary }>()
</script>
