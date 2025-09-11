<script setup lang="ts">
import { reactive, watch, computed } from 'vue';

const props = defineProps<{ roundId: string | null, disabled?: boolean }>();
const emit = defineEmits<{ (e: 'submit', payload: { wager: number; betType: string; color?: string | null; isEven?: boolean | null; number?: number | null }): void }>();

const state = reactive({
  wager: 1,
  betType: 'color',
  color: 'Red',
  isEven: true,
  number: 0,
});

watch(() => state.betType, () => {
  if (state.betType !== 'color') state.color = 'Red';
  if (state.betType !== 'colorParity') state.isEven = true;
  if (state.betType !== 'exact') state.number = 0;
});

const canSubmit = computed(() => !!props.roundId && state.wager > 0);

function submit() {
  if (!canSubmit.value) return;
  emit('submit', {
    wager: state.wager,
    betType: state.betType,
    color: state.betType === 'color' || state.betType === 'colorParity' ? state.color : null,
    isEven: state.betType === 'colorParity' ? state.isEven : null,
    number: state.betType === 'exact' ? state.number : null,
  });
}
</script>

<template>
  <form class="panel bet-form" @submit.prevent="submit">
    <h3>Hacer Apuesta</h3>
    <div class="field round" v-if="roundId">Ronda: <code>{{ roundId.slice(0,8) }}</code></div>
    <label class="field">Monto
      <input type="number" min="0.1" step="0.1" v-model.number="state.wager" :disabled="disabled" required />
    </label>
    <div class="field">
      <span>Tipo</span>
      <div class="options">
        <label><input type="radio" value="color" v-model="state.betType"> Color</label>
        <label><input type="radio" value="colorParity" v-model="state.betType"> Color + Paridad</label>
        <label><input type="radio" value="exact" v-model="state.betType"> Exacto</label>
      </div>
    </div>
    <div v-if="state.betType==='color' || state.betType==='colorParity'" class="field inline">
      <label>Color
        <select v-model="state.color" :disabled="disabled">
          <option>Red</option>
          <option>Black</option>
          <option>Green</option>
        </select>
      </label>
    </div>
    <div v-if="state.betType==='colorParity'" class="field inline">
      <label>Paridad
        <select v-model="state.isEven" :disabled="disabled">
          <option :value="true">Par</option>
          <option :value="false">Impar</option>
        </select>
      </label>
    </div>
    <div v-if="state.betType==='exact'" class="field inline">
      <label>Número
        <input type="number" min="0" max="36" v-model.number="state.number" :disabled="disabled" />
      </label>
    </div>
    <button type="submit" :disabled="!canSubmit || disabled" class="primary w-full">Apostar</button>
  </form>
</template>

<style scoped>
.bet-form { display: flex; flex-direction: column; gap: .75rem; }
.field { display: flex; flex-direction: column; gap: .35rem; font-size: .875rem; }
.field.inline { flex-direction: row; align-items: center; gap: .75rem; }
input, select { background: var(--input-bg); border: 1px solid var(--border); padding: .5rem .6rem; border-radius: 8px; font: inherit; color: inherit; }
input:focus, select:focus { outline: 2px solid var(--focus); outline-offset: 1px; }
.options { display: flex; gap: 1rem; flex-wrap: wrap; }
button.primary { background: linear-gradient(135deg,#6366f1,#4f46e5); color:#fff; border:none; padding:.75rem 1rem; border-radius:10px; cursor:pointer; font-weight:600; }
button.primary:disabled { opacity:.5; cursor:not-allowed; }
.w-full { width: 100%; }
</style>
