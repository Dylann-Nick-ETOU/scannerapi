<template>
  <section class="rounded-2xl border border-cyan-900/50 bg-night/40 p-8">
    <h3 class="text-3xl font-semibold">Recommandations</h3>
    <div class="mt-6 grid gap-4 md:grid-cols-3">
      <article v-for="item in recommendations" :key="item" class="rounded-xl border border-cyan-900/70 bg-primary/30 p-4">
        <p class="text-cyan-100">{{ item }}</p>
      </article>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { SecurityIssue } from '../types/scan'

const props = defineProps<{ issues: SecurityIssue[] }>()

const recommendations = computed(() => [...new Set(props.issues.map(x => x.recommendation))].slice(0, 6))
</script>
