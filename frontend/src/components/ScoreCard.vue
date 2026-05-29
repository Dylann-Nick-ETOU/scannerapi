<template>
  <section class="rounded-2xl border border-cyan-800/70 bg-[#032a45]/85 p-8 text-center">
    <div class="mx-auto flex h-52 w-52 items-center justify-center rounded-full border-[10px] border-warning text-center">
      <div>
        <p class="text-7xl leading-none">{{ score }}</p>
        <p class="mt-2 text-3xl text-cyan-200">/100</p>
      </div>
    </div>

    <div class="mx-auto mt-8 inline-flex items-center gap-2 rounded-xl border border-warning/50 bg-warning/10 px-4 py-2 text-warning">
      ⚠ Risque modéré
    </div>
    <p class="mt-6 text-cyan-100/80">Votre API présente quelques vulnérabilités qui nécessitent une attention</p>
  </section>
</template>

<script setup lang="ts">
defineProps<{ score: number }>()
</script>
