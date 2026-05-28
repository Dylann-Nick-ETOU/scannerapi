<template>
  <section class="rounded-2xl border border-cyan-900/50 bg-night/40 p-8">
    <h2 class="text-3xl font-semibold">Scanner une API</h2>
    <p class="mt-2 text-cyan-100/70">Entrez une URL OpenAPI/Swagger ou importez un fichier JSON/YAML.</p>

    <form class="mt-6 space-y-6" @submit.prevent="submitUrl">
      <div>
        <label class="mb-2 block text-sm text-cyan-100/80" for="openapi-url">URL Swagger/OpenAPI</label>
        <div class="flex flex-col gap-3 md:flex-row">
          <input
            id="openapi-url"
            v-model.trim="openApiUrl"
            type="url"
            placeholder="https://example.com/swagger/v1/swagger.json"
            class="w-full rounded-lg border border-cyan-700 bg-primary/60 px-4 py-3 text-cyan-50 outline-none focus:border-accent"
          />
          <button
            type="submit"
            :disabled="loading || !openApiUrl"
            class="rounded-lg bg-accent px-6 py-3 font-semibold text-night transition hover:brightness-110 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {{ loading ? 'Scan en cours...' : 'Scanner l\'URL' }}
          </button>
        </div>
      </div>
    </form>

    <div class="my-6 flex items-center gap-4 text-cyan-100/60">
      <div class="h-px flex-1 bg-cyan-900"></div>
      <span>ou</span>
      <div class="h-px flex-1 bg-cyan-900"></div>
    </div>

    <form class="space-y-4" @submit.prevent="submitFile">
      <label class="mb-2 block text-sm text-cyan-100/80" for="openapi-file">Importer un fichier OpenAPI JSON/YAML</label>
      <input
        id="openapi-file"
        type="file"
        accept=".json,.yaml,.yml,application/json,text/yaml,text/x-yaml"
        class="block w-full rounded-lg border border-cyan-700 bg-primary/60 px-4 py-3 text-cyan-50 file:mr-4 file:rounded file:border-0 file:bg-cyan-800 file:px-3 file:py-2 file:text-cyan-50"
        @change="onFileChange"
      />
      <button
        type="submit"
        :disabled="loading || !selectedFile"
        class="w-full rounded-lg border border-cyan-700 bg-primary/70 px-6 py-3 font-semibold text-cyan-100 transition hover:border-accent hover:text-accent disabled:cursor-not-allowed disabled:opacity-60"
      >
        {{ loading ? 'Scan en cours...' : 'Lancer l\'analyse du fichier' }}
      </button>
      <p v-if="selectedFile" class="text-sm text-cyan-200">Fichier sélectionné: {{ selectedFile.name }}</p>
      <p v-if="error" class="text-sm text-critical">{{ error }}</p>
    </form>
  </section>
</template>

<script setup lang="ts">
import { ref } from 'vue'

const emit = defineEmits<{
  scanUrl: [url: string]
  scanFile: [file: File]
}>()

defineProps<{ loading: boolean; error?: string }>()

const openApiUrl = ref('')
const selectedFile = ref<File | null>(null)

function submitUrl() {
  if (!openApiUrl.value) {
    return
  }

  emit('scanUrl', openApiUrl.value)
}

function onFileChange(event: Event) {
  const input = event.target as HTMLInputElement
  selectedFile.value = input.files?.[0] ?? null
}

function submitFile() {
  if (!selectedFile.value) {
    return
  }

  emit('scanFile', selectedFile.value)
}
</script>
