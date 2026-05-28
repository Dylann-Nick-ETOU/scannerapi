<template>
  <section class="grid gap-4 md:grid-cols-4">
    <article class="rounded-xl border border-red-900/40 bg-night/40 p-4">
      <p class="text-sm text-cyan-100/70">Critical</p>
      <p class="text-3xl text-critical">{{ summary.critical }}</p>
    </article>
    <article class="rounded-xl border border-orange-900/40 bg-night/40 p-4">
      <p class="text-sm text-cyan-100/70">High</p>
      <p class="text-3xl text-warning">{{ summary.high }}</p>
    </article>
    <article class="rounded-xl border border-yellow-900/40 bg-night/40 p-4">
      <p class="text-sm text-cyan-100/70">Medium</p>
      <p class="text-3xl text-accent">{{ summary.medium }}</p>
    </article>
    <article class="rounded-xl border border-cyan-900/40 bg-night/40 p-4">
      <p class="text-sm text-cyan-100/70">Low</p>
      <p class="text-3xl text-cyan-300">{{ summary.low }}</p>
    </article>
  </section>
</template>

<script setup lang="ts">
import type { ScanSummary } from '../types/scan'

defineProps<{ summary: ScanSummary }>()
</script>
