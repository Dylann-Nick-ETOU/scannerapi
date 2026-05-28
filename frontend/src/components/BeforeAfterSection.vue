<template>
  <section class="rounded-2xl border border-cyan-900/50 bg-night/40 p-8">
    <h3 class="text-3xl font-semibold">Avant / Après</h3>
    <div class="mt-6 grid gap-6 md:grid-cols-2">
      <article class="rounded-xl border border-critical/40 bg-primary/20 p-5">
        <h4 class="text-xl">API vulnérable</h4>
        <ul class="mt-4 space-y-2 text-cyan-100/80">
          <li>Pas d'authentification</li>
          <li>Pas de validation</li>
          <li>Permissions absentes</li>
          <li>Erreurs détaillées</li>
          <li>Logs inexistants</li>
        </ul>
      </article>
      <article class="rounded-xl border border-safe/40 bg-primary/20 p-5">
        <h4 class="text-xl">API sécurisée</h4>
        <ul class="mt-4 space-y-2 text-cyan-100/80">
          <li>JWT/OAuth2</li>
          <li>Validation stricte</li>
          <li>Rôles et policies</li>
          <li>Middleware global</li>
          <li>Serilog</li>
        </ul>
      </article>
    </div>
  </section>
</template>
