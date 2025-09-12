<script setup lang="ts">
import { ref, watch, onMounted, computed } from 'vue';
import type { SpinResultDto } from '../types';

/**
 * Nuevo componente de ruleta:
 * - Orden real europeo 0-36
 * - Segmentos tipo triángulo (37) y puntero fijo (clavito)
 * - Animación suave con easing + freno final
 * - Opción de color dinámico para el 0 (puede ser 'green' o 'red') sólo visual por ahora
 * - Sustituye la "bola" por el puntero fijo arriba
 */

const props = defineProps<{ spin: SpinResultDto | null; spinning: boolean; size?: number }>();
const emit = defineEmits<{ (e: 'animation-end'): void }>();

// Orden europeo estándar
const sequence = [0,32,15,19,4,21,2,25,17,34,6,27,13,36,11,30,8,23,10,5,24,16,33,1,20,14,31,9,22,18,29,7,28,12,35,3,26];

// Mapeo de colores reales (0 = verde estándar). El usuario pidió poder iniciar 0 como rojo o verde.
const baseColors: Record<number, 'red' | 'black' | 'green'> = {
  0: 'green',
  32: 'red', 15: 'black', 19: 'red', 4: 'black', 21: 'red', 2: 'black', 25: 'red', 17: 'black', 34: 'red', 6: 'black', 27: 'red', 13: 'black', 36: 'red', 11: 'black', 30: 'red', 8: 'black', 23: 'red', 10: 'black', 5: 'red', 24: 'black', 16: 'red', 33: 'black', 1: 'red', 20: 'black', 14: 'red', 31: 'black', 9: 'red', 22: 'black', 18: 'red', 29: 'black', 7: 'red', 28: 'black', 12: 'red', 35: 'black', 3: 'red', 26: 'black'
};

// Color dinámico del cero (sólo visual). Elegido al montar y cada vez que comienza un nuevo giro.
const zeroColor = ref<'green' | 'red'>(Math.random() < 0.5 ? 'green' : 'red');

// Referencia al contenedor que rota
const wheel = ref<HTMLElement | null>(null);
const currentRotation = ref(0); // estado actual
let animating = false;

const degPerSeg = 360 / sequence.length; // ~9.7297º

// Mapeo rápido número -> índice
const indexMap: Record<number, number> = Object.fromEntries(sequence.map((n,i)=>[n,i]));

// Colores efectivos (inyectando el color dinámico del 0)
const colors = computed(() => sequence.map(n => n === 0 ? zeroColor.value : baseColors[n]));

// Si cambia la bandera de spinning
watch(() => props.spinning, (val) => { if (val) tryStart(); });
// Si llega el spin después de poner spinning=true
watch(() => props.spin, (val) => { if (val && props.spinning) tryStart(); });

function tryStart() {
  if (!props.spin) return; // esperamos tener número
  startSpin();
}

function startSpin() {
  if (!props.spin) return; // necesitamos el número objetivo
  if (animating) return;
  if (!wheel.value) return;
  animating = true;
  // Si el número es 0, definimos aleatoriamente su color visible (no afecta backend todavía)
  if (props.spin.number === 0) zeroColor.value = Math.random() < 0.5 ? 'green' : 'red';

  const targetIdx = indexMap[props.spin.number];
  // Queremos que el centro del segmento objetivo quede arriba (puntero). Consegimos eso añadiendo targetIdx * degPerSeg
  // Offset para centrar (segmento empieza en idx*degPerSeg). Centro = idx*deg + deg/2. Así que rotamos total = baseSpins*360 + (targetIdx * deg) + (deg/2)
  const baseSpins = 6 + Math.random()*1.5; // 6-7.5 vueltas
  const targetRotation = baseSpins*360 + targetIdx * degPerSeg + degPerSeg/2;

  // Aplicamos easing manual (JS) para tener más control (easeOutCubic)
  const duration = 3200; // ms
  const start = performance.now();
  const from = currentRotation.value % 360; // normalizamos para evitar números gigantes
  const totalDelta = targetRotation + (from > 0 ? 360 - from : 0); // aseguramos que siempre avanza

  function easeOutCubic(t: number) { return 1 - Math.pow(1 - t, 3); }

  function frame(now: number) {
    const elapsed = now - start;
    const t = Math.min(1, elapsed / duration);
    const eased = easeOutCubic(t);
    const value = from + totalDelta * eased;
    if (wheel.value) wheel.value.style.transform = `rotate(${value}deg)`;
    if (t < 1) {
      requestAnimationFrame(frame);
    } else {
      currentRotation.value = value;
      animating = false;
      emit('animation-end');
    }
  }
  requestAnimationFrame(frame);
}

onMounted(() => { if (props.spin && props.spinning) startSpin(); });

const sizePx = computed(() => props.size && props.size > 120 ? props.size : 260);
</script>

<template>
  <div class="roulette" :style="{ width: sizePx + 'px', height: sizePx + 'px' }">
    <div class="pointer">
      <div class="pin" />
      <div class="tip" />
    </div>
    <div class="wheel" ref="wheel">
      <!-- Segmentos -->
      <div v-for="(n, i) in sequence" :key="n" class="seg" :class="colors[i]" :style="segStyle(i)">
        <span class="label" :class="{ zero: n===0 }">{{ n }}</span>
      </div>
      <div class="hub">
        <span class="brand">WT</span>
      </div>
    </div>
  </div>
</template>

<script lang="ts">
// Script adicional (no setup) sólo para exponer helper a plantilla sin recrearlo en cada render.
export default {
  methods: {
    segStyle(i: number) {
      const degPer = 360 / 37;
      return { '--rot': `${i * degPer}deg` } as Record<string,string>;
    }
  }
};
</script>

<style scoped>
.roulette { position:relative; display:flex; justify-content:center; align-items:center; }
.wheel { position:relative; width:100%; height:100%; border-radius:50%; background:#111; overflow:visible; box-shadow:0 8px 30px -6px rgba(0,0,0,.75), inset 0 0 14px #000; transition: filter .3s; }
.wheel::before { content:""; position:absolute; inset:0; border-radius:50%; background:radial-gradient(circle at 50% 50%, #1f2937 0 55%, #000 70% 100%); z-index:0; }

/* Segmentos triangulares */
.seg { position:absolute; top:50%; left:50%; width:50%; height:50%; transform-origin:0% 0%; transform: rotate(var(--rot)) skewY(calc(90deg - 360deg/37)); overflow:visible; display:flex; justify-content:flex-end; }
.seg::before { content:""; position:absolute; inset:0; transform: skewY(calc(- (90deg - 360deg/37))) rotate(calc( (360deg/37)/2 )); border-radius:2px; }
.seg.red::before { background:linear-gradient(135deg,#dc2626,#7f1d1d); }
.seg.black::before { background:linear-gradient(135deg,#0f172a,#000); }
.seg.green::before { background:linear-gradient(135deg,#065f46,#0d9488); }

.label { position:absolute; top:50%; left:100%; transform: rotate(calc( (360deg/37)/2 )) translate(-90%, -50%) rotate(calc(-1 * var(--rot))); font-size: clamp(.55rem, .9vw + .25rem, .85rem); font-weight:600; color:#fff; letter-spacing:.5px; text-shadow:0 1px 2px #000; }
.label.zero { font-weight:700; }

/* Eje central */
.hub { position:absolute; top:50%; left:50%; width:34%; height:34%; margin:-17%; border-radius:50%; background:radial-gradient(circle at 30% 30%, #334155, #111827); display:flex; justify-content:center; align-items:center; box-shadow:inset 0 0 6px #000, 0 4px 14px -6px #000; z-index:3; }
.hub .brand { font-size:1.4rem; font-weight:700; background:linear-gradient(90deg,#6366f1,#8b5cf6); -webkit-background-clip:text; color:transparent; letter-spacing:1px; }

/* Puntero fijo (clavito) */
.pointer { position:absolute; top:-4px; left:50%; transform:translateX(-50%); z-index:10; display:flex; flex-direction:column; align-items:center; pointer-events:none; }
.pointer .tip { width:0; height:0; border-left:14px solid transparent; border-right:14px solid transparent; border-bottom:20px solid #fbbf24; filter:drop-shadow(0 2px 4px rgba(0,0,0,.7)); }
.pointer .pin { width:10px; height:10px; background:#fbbf24; border-radius:50%; margin-bottom:4px; box-shadow:0 0 0 3px #92400e, 0 2px 6px -1px rgba(0,0,0,.7); }

/* Accesorios */
.wheel:after { content:""; position:absolute; inset:4%; border-radius:50%; border:4px solid rgba(255,255,255,.06); pointer-events:none; box-shadow:0 0 0 2px rgba(255,255,255,.05), 0 0 18px -4px rgba(0,0,0,.9) inset; }

/* Responsive: si el prop size es grande y la pantalla pequeña, el padre puede aplicar constraints */
</style>
