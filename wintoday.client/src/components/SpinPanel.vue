<script setup lang="ts">
import { computed } from 'vue';
import type { SpinResultDto } from '../types';

const props = defineProps<{ spin: SpinResultDto | null }>();

const colorClass = computed(() => props.spin ? `color-${props.spin.color.toLowerCase()}` : '');
</script>

<template>
  <div class="panel">
    <h3>Último Giro</h3>
    <div v-if="spin" class="spin-result" :class="colorClass">
      <div class="number">{{ spin.number }}</div>
      <div class="meta">Color: {{ spin.color }}</div>
      <div class="time">{{ new Date(spin.createdAtUtc).toLocaleTimeString() }}</div>
    </div>
    <p v-else class="empty">Aún no hay giros.</p>
  </div>
</template>

<style scoped>
.panel { background: var(--panel-bg); padding: 1rem; border-radius: 12px; box-shadow: var(--shadow-sm); }
.spin-result { display: grid; gap: .25rem; place-items: start; padding: .75rem 1rem; border-radius: 10px; color: #fff; font-weight: 600; position: relative; }
.spin-result .number { font-size: 2.5rem; line-height: 1; }
.spin-result.color-red { background: linear-gradient(135deg,#dc2626,#b91c1c); }
.spin-result.color-black { background: linear-gradient(135deg,#111827,#000); }
.spin-result.color-green { background: linear-gradient(135deg,#065f46,#059669); }
.empty { opacity: .7; font-style: italic; }
</style>
