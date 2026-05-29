<template>
  <section class="mx-auto w-full max-w-[980px] rounded-2xl border border-cyan-800/70 bg-[#032a45]/85 p-8 md:p-10">
    <h2 class="text-center text-2xl font-semibold">Scanner une API</h2>
    <p class="mt-3 text-center text-cyan-100/80">Importez votre fichier OpenAPI ou entrez une URL Swagger</p>

    <form class="mt-8" @submit.prevent="submitUrl">
      <label class="mb-3 block text-cyan-100"><span class="text-accent">⛓</span> URL Swagger/OpenAPI</label>
      <div class="flex flex-col gap-3 md:flex-row">
        <input
          id="openapi-url"
          v-model.trim="openApiUrl"
          type="url"
          placeholder="https://api.example.com/swagger.json"
          class="h-14 w-full rounded-xl border border-cyan-700 bg-[#05324f] px-4 text-lg text-cyan-50 outline-none placeholder:text-cyan-200/55 focus:border-accent"
        />
        <button
          type="submit"
          :disabled="loading || !openApiUrl"
          class="h-14 rounded-xl bg-accent px-8 text-lg font-medium text-night shadow-[0_0_16px_rgba(255,214,51,0.25)] disabled:cursor-not-allowed disabled:opacity-60"
        >
          Scanner l'URL
        </button>
      </div>
    </form>

    <div class="my-7 flex items-center gap-4 text-cyan-100/70">
      <div class="h-px flex-1 bg-cyan-800"></div>
      <span>ou</span>
      <div class="h-px flex-1 bg-cyan-800"></div>
    </div>

    <form class="space-y-4" @submit.prevent="submitFile">
      <label class="mb-3 block text-cyan-100"><span class="text-accent">↥</span> Importer un fichier OpenAPI JSON</label>

      <label
        for="openapi-file"
        class="block cursor-pointer rounded-2xl border border-dashed border-cyan-700 bg-[#04304b] px-6 py-12 text-center hover:border-cyan-500"
      >
        <div class="text-6xl leading-none text-accent">⇪</div>
        <p class="mt-3 text-2xl text-cyan-100">Cliquez pour sélectionner ou glissez un fichier</p>
        <p class="mt-1 text-cyan-200/70">Formats acceptés : JSON, YAML</p>
        <input
          id="openapi-file"
          type="file"
          accept=".json,.yaml,.yml,application/json,text/yaml,text/x-yaml"
          class="hidden"
          @change="onFileChange"
        />
      </label>

      <button
        type="submit"
        :disabled="loading || !selectedFile"
        class="h-14 w-full rounded-xl bg-[#0a4a72] text-xl font-medium text-cyan-50 disabled:cursor-not-allowed disabled:opacity-60"
      >
        {{ loading ? 'Scan en cours...' : 'Lancer l\'analyse' }}
      </button>

      <p v-if="selectedFile" class="text-sm text-cyan-200">Fichier: {{ selectedFile.name }}</p>
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
  if (openApiUrl.value) emit('scanUrl', openApiUrl.value)
}

function onFileChange(event: Event) {
  const input = event.target as HTMLInputElement
  selectedFile.value = input.files?.[0] ?? null
}

function submitFile() {
  if (selectedFile.value) emit('scanFile', selectedFile.value)
}
</script>
