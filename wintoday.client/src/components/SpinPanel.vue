<script setup lang="ts">
import { computed } from 'vue';
import type { SpinResultDto } from '../types';

const props = defineProps<{ spin: SpinResultDto | null }>();

// Backend puede serializar enums como enteros o strings según config; aseguramos string
function normalizeColor(val: unknown): string {
  if (val == null) return '';
  if (typeof val === 'string') return val;
  // Map posibles valores numéricos del enum (1 Red, 2 Black, 3 Green?)
  if (typeof val === 'number') {
    switch (val) {
      case 1: return 'Red';
      case 2: return 'Black';
      case 3: return 'Green';
    }
  }
  return String(val);
}

const colorName = computed(() => normalizeColor(props.spin?.color));
const colorClass = computed(() => colorName.value ? `color-${colorName.value.toLowerCase()}` : '');
</script>

<template>
  <div class="panel">
    <h3>Último Giro</h3>
    <div v-if="spin" class="spin-result" :class="colorClass">
      <div class="number">{{ spin.number }}</div>
      <div class="meta">Color: {{ colorName }}</div>
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
