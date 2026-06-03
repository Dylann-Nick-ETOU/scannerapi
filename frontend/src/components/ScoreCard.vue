<template>
  <section class="rounded-2xl border border-cyan-800/70 bg-[#032a45]/85 p-8 text-center">
    <div class="mx-auto flex h-52 w-52 items-center justify-center rounded-full border-[10px] border-warning text-center">
      <div>
        <p class="text-5xl leading-none">{{ score }}</p>
        <p class="mt-2 text-3xl text-cyan-200">/100</p>
      </div>
    </div>

    <div
      class="mx-auto mt-8 inline-flex items-center gap-2 rounded-xl border px-4 py-2"
      :class="badgeClass"
    >
      {{ levelLabel }}
    </div>
    <p class="mt-6 text-cyan-100/80">{{ levelDescription }}</p>
  </section>
</template>

<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{ score: number }>()

const levelLabel = computed(() => {
  if (props.score >= 90) return 'Sécurité très bonne'
  if (props.score >= 70) return 'Niveau correct'
  if (props.score >= 50) return 'Risque modéré'
  return 'Niveau critique'
})

const levelDescription = computed(() => {
  if (props.score >= 90) return 'Votre API suit globalement de très bonnes pratiques de sécurité.'
  if (props.score >= 70) return 'Quelques améliorations sont recommandées pour renforcer la sécurité.'
  if (props.score >= 50) return 'Votre API présente des vulnérabilités qui nécessitent une attention.'
  return 'Votre API est exposée à des risques majeurs, une correction prioritaire est nécessaire.'
})

const badgeClass = computed(() => {
  if (props.score >= 90) return 'border-safe/50 bg-safe/10 text-safe'
  if (props.score >= 70) return 'border-cyan-500/50 bg-cyan-500/10 text-cyan-200'
  if (props.score >= 50) return 'border-warning/50 bg-warning/10 text-warning'
  return 'border-critical/50 bg-critical/10 text-critical'
})
</script>
