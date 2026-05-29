<template>
  <section class="rounded-2xl border border-cyan-800/70 bg-[#032a45]/85 p-8">
    <h3 class="text-center text-2xl font-semibold">Avant / Après</h3>
    <p class="mt-2 text-center text-cyan-100/80">Transformation de votre API</p>

    <div class="mt-10 grid gap-8 md:grid-cols-[1fr_auto_1fr] md:items-center">
      <article class="rounded-2xl border border-critical/40 bg-[#042f4b] p-6">
        <div class="mx-auto flex h-20 w-20 items-center justify-center rounded-full bg-critical/10 text-2xl text-critical">✕</div>
        <h4 class="mt-4 text-center text-2xl">API vulnérable</h4>
        <p class="mt-1 text-center text-cyan-100/75">État actuel de votre API</p>
        <ul class="mt-6 space-y-3">
          <li v-for="item in bad" :key="item" class="rounded-xl border border-critical/30 bg-[#0f2f49] px-4 py-3">✕ {{ item }}</li>
        </ul>
      </article>

      <div class="mx-auto flex h-24 w-24 items-center justify-center rounded-full border-4 border-accent/35 text-6xl text-accent">➜</div>

      <article class="rounded-2xl border border-safe/40 bg-[#042f4b] p-6">
        <div class="mx-auto flex h-20 w-20 items-center justify-center rounded-full bg-safe/15 text-2xl text-safe">✓</div>
        <h4 class="mt-4 text-center text-2xl">API sécurisée</h4>
        <p class="mt-1 text-center text-cyan-100/75">Après implémentation</p>
        <ul class="mt-6 space-y-3">
          <li v-for="item in good" :key="item" class="rounded-xl border border-safe/30 bg-[#07374f] px-4 py-3">✓ {{ item }}</li>
        </ul>
      </article>
    </div>

    <div class="mt-8 text-center">
      <span class="rounded-2xl border border-accent/50 bg-accent/10 px-6 py-3 text-cyan-100">Analyse → Correction → Sécurisation</span>
    </div>
  </section>
</template>

<script setup lang="ts">
const bad = ['Pas d\'authentification', 'Pas de validation', 'Permissions absentes', 'Erreurs détaillées', 'Logs inexistants']
const good = ['JWT/OAuth2', 'Validation stricte', 'Rôles et policies', 'Middleware global d\'erreurs', 'Logs utiles avec Serilog']
</script>
