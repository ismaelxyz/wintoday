<script setup lang="ts">
import { ref, watch, onMounted } from 'vue';
import type { SpinResultDto } from '../types';

const props = defineProps<{ spin: SpinResultDto | null; spinning: boolean }>();
const emit = defineEmits<{ (e: 'animation-end'): void }>();

const wheel = ref<HTMLElement | null>(null);
const ball = ref<HTMLElement | null>(null);

// Simple mapping: numbers 0-36 placed sequentially (not real roulette order)
const slots = Array.from({ length: 37 }, (_, i) => i);

let animationRunning = false;

watch(() => props.spinning, (val) => {
  if (val) startAnimation();
});

function startAnimation() {
  if (!wheel.value || !ball.value || animationRunning) return;
  animationRunning = true;
  const rotations = 6 + Math.random() * 2; // base spins
  const duration = 2600; // ms
  const target = props.spin?.number ?? Math.floor(Math.random()*37);
  const degPerSlot = 360/37;
  const offset = target * degPerSlot + degPerSlot/2; // center of slot
  const finalRotation = rotations * 360 + offset;

  wheel.value.animate([
    { transform: 'rotate(0deg)' },
    { transform: `rotate(${finalRotation}deg)` }
  ], { duration, easing: 'cubic-bezier(.25,.8,.3,1)' });

  ball.value.animate([
    { transform: 'rotate(0deg)' },
    { transform: `rotate(${-finalRotation * 1.3}deg)` }
  ], { duration, easing: 'cubic-bezier(.25,.8,.3,1)' });

  setTimeout(() => { animationRunning = false; emit('animation-end'); }, duration);
}

onMounted(() => { if (props.spinning) startAnimation(); });
</script>

<template>
  <div class="roulette">
    <div class="wheel" ref="wheel">
      <div v-for="n in slots" :key="n" class="slot" :class="{ red: n%2===1, black: n%2===0 && n!==0, green: n===0 }" :style="{ '--i': n }">{{ n }}</div>
      <div class="ball" ref="ball" />
    </div>
    <div class="marker" />
  </div>
</template>

<style scoped>
.roulette { position:relative; width:220px; height:220px; }
.wheel { position:relative; width:100%; height:100%; border-radius:50%; background:#111; overflow:hidden; box-shadow:0 8px 25px -6px rgba(0,0,0,.7), inset 0 0 8px #000; }
.slot { position:absolute; top:50%; left:50%; width:50%; height:14%; transform-origin:0% 50%; display:flex; align-items:center; justify-content:flex-end; padding-right:8px; font-size:.7rem; font-weight:600; color:#fff; letter-spacing:.5px; }
.slot { transform:rotate(calc(var(--i)* (360deg/37))) translateX(-50%); }
.slot.red { background:#b91c1c; }
.slot.black { background:#111827; }
.slot.green { background:#166534; }
.ball { position:absolute; top:50%; left:50%; width:18px; height:18px; margin:-9px; background:#f8fafc; border-radius:50%; box-shadow:0 0 0 3px #fff8,0 0 8px 2px #fff9; }
.marker { position:absolute; top:-6px; left:50%; width:0; height:0; border:10px solid transparent; border-bottom-color:#fbbf24; transform:translateX(-50%); filter:drop-shadow(0 2px 4px #000); }
</style>
