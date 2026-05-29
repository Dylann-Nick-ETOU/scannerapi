<template>
  <section class="grid gap-10 lg:grid-cols-[1.05fr_0.95fr] lg:items-center">
    <div>
      <h1 class="max-w-xl text-5xl font-semibold leading-tight tracking-[0.01em] text-white md:text-6xl">
        Analysez la sécurité<br />de vos API en<br />quelques secondes
      </h1>

      <p class="mt-8 max-w-2xl text-xl leading-relaxed text-cyan-100/85 md:text-2xl">
        Détectez les mauvaises pratiques courantes dans vos API à partir d'un fichier OpenAPI/Swagger
        et obtenez des recommandations concrètes.
      </p>

      <div class="mt-10 flex flex-wrap gap-4">
        <button
          class="rounded-2xl bg-accent px-9 py-4 text-lg font-semibold text-night shadow-[0_0_18px_rgba(255,214,51,0.35)] transition hover:brightness-110"
          @click="$emit('start')"
        >
          Lancer un scan
        </button>
        <button
          class="rounded-2xl border border-cyan-700 bg-transparent px-9 py-4 text-lg font-medium text-cyan-100 transition hover:border-cyan-500"
          @click="$emit('demo')"
        >
          Voir une démo
        </button>
      </div>
    </div>

    <div class="rounded-3xl border border-cyan-900/70 bg-[#012a45]/90 p-7 shadow-[0_20px_40px_rgba(0,0,0,0.25)]">
      <div class="space-y-7">
        <div class="grid grid-cols-[44px_1fr_auto] items-center gap-4">
          <div class="flex h-12 w-12 items-center justify-center rounded-xl bg-[#053251] text-accent">▭</div>
          <div class="h-2.5 rounded-full bg-[#294d58]"><div class="h-full w-4/5 rounded-full bg-accent"></div></div>
          <span class="text-lg text-accent">API</span>
        </div>

        <div class="grid grid-cols-[44px_1fr_auto] items-center gap-4">
          <div class="flex h-12 w-12 items-center justify-center rounded-xl bg-[#053251] text-accent">›_</div>
          <div class="h-2.5 rounded-full bg-[#294d58]"><div class="h-full w-[46%] rounded-full bg-accent"></div></div>
          <span class="text-lg text-accent">Scanner</span>
        </div>

        <div class="grid grid-cols-[44px_1fr_auto] items-center gap-4">
          <div class="flex h-12 w-12 items-center justify-center rounded-xl bg-[#053251] text-accent">🔒</div>
          <div class="h-2.5 rounded-full bg-[#294d58]"><div class="h-full w-full rounded-full bg-accent"></div></div>
          <span class="text-lg text-accent">Rapport</span>
        </div>
      </div>

      <div class="mt-8 rounded-2xl border border-cyan-700/70 bg-[#062f49] px-5 py-4 text-lg text-cyan-100">
        Analyse sécurisée et confidentielle
      </div>
    </div>
  </section>
</template>

<script setup lang="ts">
defineEmits<{ start: []; demo: [] }>()
</script>
