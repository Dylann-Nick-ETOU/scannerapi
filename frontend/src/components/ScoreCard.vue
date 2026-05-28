<template>
  <section class="rounded-2xl border border-cyan-900/50 bg-night/40 p-8">
    <h3 class="text-xl text-cyan-100/70">Score de sécurité</h3>
    <p class="mt-3 text-6xl font-semibold text-accent">{{ score }}</p>
    <p class="text-xl text-cyan-200">/100</p>
  </section>
</template>

<script setup lang="ts">
defineProps<{ score: number }>()
</script>
