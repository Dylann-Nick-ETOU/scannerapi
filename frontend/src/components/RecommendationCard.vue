<template>
  <section class="space-y-8">
    <article v-if="topIssue" class="rounded-2xl border border-warning/40 bg-[#032a45]/90">
      <header class="rounded-t-2xl border-b border-warning/35 bg-warning/10 px-8 py-6">
        <h3 class="text-2xl font-semibold">{{ topIssue.title }} <span class="ml-2 rounded-full border border-warning/60 px-3 py-1 text-xs text-warning">{{ topIssue.severity }}</span></h3>
        <p class="mt-2 text-cyan-100/80">Détecté dans l'analyse de sécurité de votre API</p>
      </header>
      <div class="space-y-6 px-8 py-6 text-cyan-100">
        <div class="grid gap-4 md:grid-cols-2 text-sm">
          <p><strong>Endpoint concerné:</strong> <span class="rounded bg-[#12384e] px-2 py-1 font-mono">{{ topIssue.endpoint }}</span></p>
          <p><strong>Catégorie OWASP:</strong> {{ topIssue.owaspCategory }}</p>
          <p><strong>Risque:</strong> {{ topIssue.description }}</p>
          <p><strong>Code règle:</strong> <span class="rounded bg-[#355d38] px-2 py-1 font-mono text-accent">{{ topIssue.ruleCode }}</span></p>
        </div>
        <div class="rounded-xl border border-cyan-700/70 bg-[#0a3c57] p-5">
          <p class="text-lg font-medium">Recommandation</p>
          <p class="mt-2 text-sm">{{ topIssue.recommendation }}</p>
          <pre class="mt-4 overflow-x-auto rounded bg-[#082e44] p-4 text-xs text-cyan-100">[Authorize(Roles = "Admin")]
public async Task&lt;IActionResult&gt; GetUsers()</pre>
        </div>
      </div>
    </article>

    <article class="rounded-2xl border border-cyan-800/70 bg-[#032a45]/85 p-8">
      <h3 class="text-center text-4xl font-semibold">Recommandations</h3>
      <p class="mt-2 text-center text-cyan-100/80">Actions prioritaires pour sécuriser votre API</p>

      <div class="mt-8 grid gap-5 md:grid-cols-3">
        <div v-for="item in recommendations" :key="item" class="rounded-2xl border border-cyan-700/70 bg-[#04304b] p-5">
          <div class="mb-3 flex h-12 w-12 items-center justify-center rounded-xl bg-[#12384e] text-xl" :class="iconColor(item)">
            {{ iconFor(item) }}
          </div>
          <h4 class="text-xl font-medium leading-tight text-cyan-50">{{ item }}</h4>
          <p class="mt-2 text-sm leading-relaxed text-cyan-100/75">{{ helperText(item) }}</p>
        </div>
      </div>
    </article>
  </section>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { SecurityIssue } from '../types/scan'

const props = defineProps<{ issues: SecurityIssue[] }>()
const topIssue = computed(() => props.issues[0])
const recommendations = computed(() => {
  const defaults = ['Ajouter JWT/OAuth2', 'Valider toutes les entrées', 'Masquer les champs sensibles', 'Centraliser la gestion des erreurs', 'Activer HTTPS', 'Journaliser les événements importants']
  const fromIssues = props.issues.map(x => x.recommendation)
  return [...new Set([...fromIssues, ...defaults])].slice(0, 6)
})
function iconFor(text: string): string {
  const t = text.toLowerCase()
  if (t.includes('jwt') || t.includes('oauth')) return '🔒'
  if (t.includes('valider')) return '✅'
  if (t.includes('masquer')) return '👁'
  if (t.includes('erreur')) return '❗'
  if (t.includes('https')) return '🛡'
  return '📄'
}
function iconColor(text: string): string {
  const t = text.toLowerCase()
  if (t.includes('jwt') || t.includes('oauth') || t.includes('masquer')) return 'text-accent'
  if (t.includes('valider') || t.includes('https')) return 'text-safe'
  if (t.includes('erreur')) return 'text-critical'
  return 'text-cyan-200'
}
function helperText(text: string): string {
  const t = text.toLowerCase()
  if (t.includes('jwt') || t.includes('oauth')) return 'Implémenter une authentification robuste pour tous les endpoints sensibles'
  if (t.includes('valider')) return 'Utiliser des schemas de validation pour prévenir les injections'
  if (t.includes('masquer')) return 'Ne jamais exposer mots de passe, tokens ou données personnelles'
  if (t.includes('erreur')) return 'Middleware global pour gérer les exceptions sans exposer de détails'
  if (t.includes('https')) return 'Forcer le chiffrement des communications pour toutes les requêtes'
  return 'Logger les accès, erreurs et actions critiques avec Serilog'
}
</script>
