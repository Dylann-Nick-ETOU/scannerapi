<template>
  <section class="mx-auto w-full max-w-[460px]">
    <article class="rounded-2xl border border-cyan-800/70 bg-[#032a45]/85 p-8 md:p-10">
      <h2 class="text-3xl font-semibold text-cyan-50">
        {{ mode === 'login' ? 'Connexion' : 'Créer un compte' }}
      </h2>

      <form v-if="mode === 'login'" class="mt-6 space-y-4" @submit.prevent="submitLogin">
        <input
          v-model.trim="loginUsername"
          type="text"
          placeholder="Nom d'utilisateur"
          class="h-12 w-full rounded-xl border border-cyan-700 bg-[#05324f] px-4 text-cyan-50 outline-none placeholder:text-cyan-200/55 focus:border-accent"
        />
        <input
          v-model="loginPassword"
          type="password"
          placeholder="Mot de passe"
          class="h-12 w-full rounded-xl border border-cyan-700 bg-[#05324f] px-4 text-cyan-50 outline-none placeholder:text-cyan-200/55 focus:border-accent"
        />
        <button
          type="submit"
          :disabled="loading"
          class="h-12 w-full rounded-xl bg-accent px-4 font-medium text-night disabled:cursor-not-allowed disabled:opacity-60"
        >
          {{ loading ? 'Connexion...' : 'Se connecter' }}
        </button>
      </form>

      <form v-else class="mt-6 space-y-4" @submit.prevent="submitRegister">
        <input
          v-model.trim="registerUsername"
          type="text"
          placeholder="Nom d'utilisateur"
          class="h-12 w-full rounded-xl border border-cyan-700 bg-[#05324f] px-4 text-cyan-50 outline-none placeholder:text-cyan-200/55 focus:border-accent"
        />
        <input
          v-model="registerPassword"
          type="password"
          placeholder="Mot de passe"
          class="h-12 w-full rounded-xl border border-cyan-700 bg-[#05324f] px-4 text-cyan-50 outline-none placeholder:text-cyan-200/55 focus:border-accent"
        />
        <input
          v-model="registerConfirmPassword"
          type="password"
          placeholder="Confirmer le mot de passe"
          class="h-12 w-full rounded-xl border border-cyan-700 bg-[#05324f] px-4 text-cyan-50 outline-none placeholder:text-cyan-200/55 focus:border-accent"
        />
        <button
          type="submit"
          :disabled="loading"
          class="h-12 w-full rounded-xl bg-accent px-4 font-medium text-night disabled:cursor-not-allowed disabled:opacity-60"
        >
          {{ loading ? 'Création...' : 'Créer le compte' }}
        </button>
      </form>

      <p v-if="error" class="mt-4 rounded-xl border border-critical/40 bg-critical/10 px-4 py-3 text-sm text-critical">
        {{ error }}
      </p>

      <p class="mt-6 text-sm text-cyan-200/80">
        <template v-if="mode === 'login'">
          Vous n'avez pas de compte ?
          <button class="text-accent hover:underline" @click="mode = 'register'">Créer un compte</button>
        </template>
        <template v-else>
          Vous avez déjà un compte ?
          <button class="text-accent hover:underline" @click="mode = 'login'">Se connecter</button>
        </template>
      </p>
    </article>
  </section>
</template>

<script setup lang="ts">
import { ref } from 'vue'

defineProps<{
  loading: boolean
  error?: string
}>()

const emit = defineEmits<{
  login: [payload: { username: string; password: string }]
  register: [payload: { username: string; password: string; confirmPassword: string }]
}>()

const mode = ref<'login' | 'register'>('login')
const loginUsername = ref('')
const loginPassword = ref('')
const registerUsername = ref('')
const registerPassword = ref('')
const registerConfirmPassword = ref('')

function submitLogin() {
  if (!loginUsername.value || !loginPassword.value) {
    return
  }

  emit('login', {
    username: loginUsername.value,
    password: loginPassword.value
  })
}

function submitRegister() {
  if (!registerUsername.value || !registerPassword.value || !registerConfirmPassword.value) {
    return
  }

  emit('register', {
    username: registerUsername.value,
    password: registerPassword.value,
    confirmPassword: registerConfirmPassword.value
  })
}
</script>
