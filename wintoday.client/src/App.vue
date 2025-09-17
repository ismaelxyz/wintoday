<script setup lang="ts">
import { reactive, computed, ref, watch } from "vue";
import { api } from "./api";
import type { PlayerBalanceDto, SpinResultDto, BetOutcomeDto, SaveSessionBetItem } from "./types";

import BetForm from "./components/BetForm.vue";

interface HistoryEntry {
  spin: SpinResultDto;
  bet?: BetOutcomeDto | (BetOutcomeDto & { unsaved?: boolean });
  unsaved?: boolean; // flag to style unsaved entries
}

interface State {
  player: PlayerBalanceDto | null;
  lastSpin: SpinResultDto | null; // current spin result before betting
  loading: boolean;
  error: string | null;
  history: HistoryEntry[]; // mixed persisted + unsaved
  sessionBets: (SaveSessionBetItem & { clientId: string; won?: boolean; profit?: number })[];
  sessionFundsPreview: number | null;
  saving: boolean;
  loggingIn: boolean;
}

const state = reactive<State>({
  player: null,
  lastSpin: null,
  loading: false,
  error: null,
  history: [],
  sessionBets: [],
  sessionFundsPreview: null,
  saving: false,
  loggingIn: false,
});

async function login(name: string) {
  state.error = null;
  state.loading = true;
  state.loggingIn = true;
  try {
    state.player = await api.login({ playerName: name.trim() });
    if (state.player) {
      const hist = await api.betHistory(state.player.name, 100);
      state.history = hist.map((h) => ({
        spin: {
          roundId: h.roundId,
          number: h.number,
          color: h.color,
          createdAtUtc: h.roundCreatedAtUtc,
        },
        bet: {
          roundId: h.roundId,
          betId: h.betId,
          number: h.number,
          color: h.color,
          wager: h.wager,
          profit: h.profit,
          newBalance: state.player!.funds,
          won: h.won,
          betType: h.betType,
          criteria: h.criteria,
        },
      }));
      // Reset session (norma 7)
      state.sessionBets = [];
      state.sessionFundsPreview = state.player.funds;
      state.lastSpin = null;
    }
  } catch (e: unknown) {
    const msg =
      e && typeof e === "object" && "message" in e ? (e as { message?: string }).message : "Error";
    state.error = msg || "Error";
  } finally {
    state.loading = false;
    state.loggingIn = false;
  }
}

const wheelSpinning = ref(false);
interface WheelExpose {
  launchWheel?: () => void;
  reset?: () => void;
}
const wheelRef = ref<WheelExpose | null>(null);
// Secuencia estándar europea 0-36
const rouletteSequence = [
  0, 32, 15, 19, 4, 21, 2, 25, 17, 34, 6, 27, 13, 36, 11, 30, 8, 23, 10, 5, 24, 16, 33, 1, 20, 14,
  31, 9, 22, 18, 29, 7, 28, 12, 35, 3, 26,
];
interface RouletteItem {
  id: number;
  name: string;
  htmlContent: string;
  textColor?: string;
  background?: string;
}
function isRed(num: number) {
  return new Set([32, 19, 21, 25, 34, 27, 36, 30, 23, 5, 16, 1, 14, 9, 18, 7, 12, 3]).has(num);
}
const rouletteItems: RouletteItem[] = rouletteSequence.map((n) => ({
  id: n,
  name: String(n),
  htmlContent: `<span>${n}</span>`,
  textColor: "#fff",
  background: n === 0 ? "#0d9488" : isRed(n) ? "#b91c1c" : "#111827",
}));
// Adaptar a la API de vue3-roulette: objeto { value }
const wheelResultIndex = ref<{ value: number | null }>({ value: null });
watch(
  () => state.lastSpin,
  (val) => {
    if (val) {
      wheelResultIndex.value.value = rouletteSequence.indexOf(val.number);
      if (wheelSpinning.value) setTimeout(() => wheelRef.value?.launchWheel?.(), 40);
    }
  }
);
function onWheelStart() {
  /* opcional: sonido */
}
function onWheelEnd() {
  wheelSpinning.value = false;
  onWheelAnimationEnd();
  state.lastSpin = null;
  wheelResultIndex.value.value = null;
}

async function ensureSpin(): Promise<SpinResultDto | null> {
  if (!state.player || wheelSpinning.value) return null;
  state.error = null;
  wheelSpinning.value = true;
  try {
    const result = await api.spin(state.player.name);
    state.lastSpin = result;
    // El watcher sobre lastSpin ajustará wheelResultIndex y luego lanzará.
    setTimeout(() => wheelRef.value?.launchWheel?.(), 140);
    return result;
  } catch (e: unknown) {
    const msg =
      e && typeof e === "object" && "message" in e ? (e as { message?: string }).message : "Error";
    state.error = msg || "Error";
    wheelSpinning.value = false;
    return null;
  }
}

async function placeBet(payload: {
  wager: number;
  betType: string;
  color?: string | null;
  isEven?: boolean | null;
  number?: number | null;
}) {
  if (!state.player) return;
  state.error = null;
  if (!state.lastSpin) {
    const s = await ensureSpin();
    if (!s) return;
  }
  if (!state.lastSpin) return;
  const spin = state.lastSpin;
  const bet: SaveSessionBetItem & { clientId: string; won?: boolean; profit?: number } = {
    wager: payload.wager,
    betType: payload.betType,
    color: payload.color ?? null,
    isEven: payload.isEven ?? null,
    number: payload.number ?? null,
    numberResult: spin.number,
    colorResult: spin.color,
    playedAtUtc: new Date().toISOString(),
    clientId: crypto.randomUUID(),
  };
  state.sessionBets.push(bet);
  if (state.sessionFundsPreview == null && state.player)
    state.sessionFundsPreview = state.player.funds;
  // Deduct wager first
  if (state.sessionFundsPreview != null) state.sessionFundsPreview -= bet.wager;
  // Evaluate win locally
  const isEven = spin.number % 2 === 0;
  let won = false;
  let profit = 0;
  switch (bet.betType) {
    case "color":
      won = bet.color?.toLowerCase() === spin.color.toLowerCase();
      profit = won ? bet.wager / 2 : 0;
      break;
    case "colorParity":
      won = bet.color?.toLowerCase() === spin.color.toLowerCase() && bet.isEven === isEven;
      profit = won ? bet.wager : 0;
      break;
    case "exact":
      won = bet.color?.toLowerCase() === spin.color.toLowerCase() && bet.number === spin.number;
      profit = won ? bet.wager * 3 : 0;
      break;
  }
  if (won && state.sessionFundsPreview != null) state.sessionFundsPreview += bet.wager + profit;
  bet.won = won;
  bet.profit = profit;
  // Add a provisional history entry (unsaved)
  state.history.unshift({
    spin: {
      roundId: `tmp-${bet.clientId}`,
      number: spin.number,
      color: spin.color,
      createdAtUtc: spin.createdAtUtc,
    },
    bet: {
      roundId: `tmp-${bet.clientId}`,
      betId: `tmp-${bet.clientId}`,
      number: spin.number,
      color: spin.color,
      wager: bet.wager,
      profit: profit,
      newBalance: state.sessionFundsPreview ?? 0,
      won: won,
      betType: bet.betType,
      criteria: { color: bet.color, isEven: bet.isEven, number: bet.number },
      unsaved: true,
    } as BetOutcomeDto & { unsaved: boolean },
    unsaved: true,
  });
  // Clear spin to allow rapid new spin
  state.lastSpin = null;
  wheelSpinning.value = false;
}

async function saveSession() {
  if (!state.player || state.sessionBets.length === 0) return;
  state.saving = true;
  state.error = null;
  try {
    const res = await api.saveSession({ playerName: state.player.name, bets: state.sessionBets });
    state.player.funds = res.endingFunds;
    // Remove all unsaved provisional entries
    state.history = state.history.filter((h) => !h.unsaved);
    // Prepend new persisted outcomes
    for (const o of res.outcomes.slice().reverse()) {
      state.history.unshift({
        spin: {
          roundId: o.roundId,
          number: o.number,
          color: o.color,
          createdAtUtc: new Date().toISOString(),
        },
        bet: o,
      });
    }
    state.sessionBets = [];
    state.sessionFundsPreview = state.player.funds;
  } catch (e: unknown) {
    const msg =
      e && typeof e === "object" && "message" in e
        ? (e as { message?: string }).message
        : "Error guardando sesión";
    state.error = msg || "Error guardando sesión";
  } finally {
    state.saving = false;
  }
}

function onWheelAnimationEnd() {
  wheelSpinning.value = false;
}
const canBet = computed(
  () => !!state.player && !wheelSpinning.value && !state.saving && !state.loggingIn
);
const loginName = reactive({ value: "" });
const hasSession = computed(() => state.sessionBets.length > 0);
const sessionFundsDisplay = computed(() => state.sessionFundsPreview ?? state.player?.funds ?? 0);

function betWager(b: HistoryEntry["bet"]): number {
  if (!b) return 0;
  return (b as BetOutcomeDto).wager;
}
</script>

<template>
  <main class="app-root">
    <header class="site-header" v-if="state.player">
      <div class="brand">WinToday <span class="tag">Demo</span></div>
      <div class="player-box" v-if="state.player">
        <span class="player-name">👤 {{ state.player.name }}</span>
        <span class="funds" :title="hasSession ? 'Incluye apuestas no guardadas' : ''"
          >💰 {{ sessionFundsDisplay.toFixed(2) }}<small v-if="hasSession">*</small></span
        >
      </div>
    </header>

    <section v-if="!state.player" class="landing">
      <div class="hero">
        <h1>WinToday Roulette</h1>
        <p class="tagline">Gira la ruleta, apuesta y mira si la suerte está contigo.</p>
        <form @submit.prevent="login(loginName.value)" class="login-form">
          <input
            v-model.trim="loginName.value"
            placeholder="Tu nombre"
            required
            :disabled="state.loading"
          />
          <button class="primary" :disabled="!loginName.value || state.loading">Entrar</button>
        </form>
        <p class="hint">Se crea tu jugador si no existe.</p>
      </div>
    </section>

    <section v-else class="dashboard">
      <div class="game-wrapper">
        <div class="panel game-panel">
          <h3>Ruleta & Apuesta</h3>
          <div class="wheel-wrapper">
            <Roulette
              ref="wheelRef"
              :items="rouletteItems"
              :wheel-result-index="wheelResultIndex"
              :size="260"
              :duration="4"
              display-indicator
              centered-indicator
              easing="ease"
              @wheel-start="onWheelStart"
              @wheel-end="onWheelEnd"
            />
          </div>
          <div class="spin-meta" v-if="state.lastSpin">
            <span class="pill num"># {{ state.lastSpin.number }}</span>
            <span class="pill col">{{ state.lastSpin.color }}</span>
            <span class="pill time">{{
              new Date(state.lastSpin.createdAtUtc).toLocaleTimeString()
            }}</span>
          </div>
          <BetForm
            :round-id="state.lastSpin?.roundId || ''"
            :disabled="!canBet"
            @submit="placeBet"
          />
          <div class="session-controls" v-if="hasSession">
            <button class="primary" :disabled="state.saving" @click="saveSession">
              💾 Guardar ({{ state.sessionBets.length }})
            </button>
          </div>
        </div>
        <div class="panel history-panel">
          <h3>Historial de Apuestas</h3>
          <ul class="bet-history-list">
            <li v-for="h in state.history" :key="h.spin.roundId" :class="{ unsaved: h.unsaved }">
              <span class="id">{{
                h.spin.roundId.startsWith("tmp-") ? "•••" : h.spin.roundId.slice(0, 6)
              }}</span>
              <span class="num">{{ h.spin.number }}</span>
              <span class="colr">{{ h.spin.color }}</span>
              <span class="res" :class="[h.bet?.won ? 'won' : 'lost', h.unsaved ? 'pending' : '']">
                <template v-if="h.bet">{{
                  h.bet.won ? "+" + h.bet.profit.toFixed(2) : "-" + betWager(h.bet).toFixed(2)
                }}</template>
              </span>
              <span class="flag" v-if="h.unsaved">(no guardada)</span>
            </li>
          </ul>
          <p class="legend" v-if="hasSession">
            * Apuestas marcadas como "no guardada" aún no impactan tu saldo real.
          </p>
        </div>
      </div>
    </section>

    <transition name="fade">
      <div v-if="state.loading && state.loggingIn" class="loading-overlay">Entrando...</div>
    </transition>
    <transition name="fade">
      <div v-if="state.saving" class="loading-overlay">Guardando sesión...</div>
    </transition>
    <transition name="fade">
      <div v-if="state.error" class="toast error">{{ state.error }}</div>
    </transition>
  </main>
</template>

<style scoped>
.app-root {
  min-height: 100dvh;
  background: radial-gradient(circle at 30% 20%, #1e1b4b, #020617);
  color: #f1f5f9;
  font-family: system-ui, "Segoe UI", Roboto, sans-serif;
}
.site-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0.85rem 1.25rem;
  background: rgba(0, 0, 0, 0.25);
  backdrop-filter: blur(8px);
  position: sticky;
  top: 0;
  z-index: 10;
}
.brand {
  font-weight: 700;
  font-size: 1.1rem;
  letter-spacing: 0.5px;
}
.brand .tag {
  font-size: 0.65rem;
  margin-left: 0.4rem;
  background: #6366f1;
  padding: 0.2rem 0.4rem;
  border-radius: 4px;
  text-transform: uppercase;
}
.player-box {
  display: flex;
  gap: 1rem;
  font-size: 0.9rem;
}
.funds {
  font-weight: 600;
}
.landing {
  display: grid;
  place-items: center;
  padding: 4rem 1rem 2rem;
}
.hero {
  max-width: 560px;
  text-align: center;
  display: flex;
  flex-direction: column;
  gap: 1.2rem;
  background: rgba(255, 255, 255, 0.04);
  padding: 2.5rem 2rem 3rem;
  border-radius: 28px;
  box-shadow: 0 10px 35px -10px rgba(0, 0, 0, 0.6), inset 0 0 0 1px rgba(255, 255, 255, 0.05);
  position: relative;
}
h1 {
  font-size: clamp(2.2rem, 5.5vw, 3.3rem);
  background: linear-gradient(90deg, #818cf8, #c084fc);
  -webkit-background-clip: text;
  background-clip: text;
  color: transparent;
  margin: 0;
}
.tagline {
  margin: 0;
  font-size: 1.05rem;
  line-height: 1.3;
  opacity: 0.85;
}
.login-form {
  display: flex;
  gap: 0.75rem;
  justify-content: center;
}
input {
  background: rgba(255, 255, 255, 0.1);
  border: 1px solid rgba(255, 255, 255, 0.2);
  padding: 0.85rem 1rem;
  border-radius: 14px;
  color: #fff;
  font-size: 0.95rem;
  width: 55%;
  min-width: 180px;
}
input:focus {
  outline: 2px solid #6366f1;
  outline-offset: 2px;
}
button.primary {
  background: linear-gradient(90deg, #6366f1, #8b5cf6);
  border: none;
  color: #fff;
  padding: 0.9rem 1.4rem;
  border-radius: 14px;
  font-weight: 600;
  cursor: pointer;
  box-shadow: 0 4px 18px -4px rgba(99, 102, 241, 0.7);
}
button.primary:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
.hint {
  font-size: 0.7rem;
  letter-spacing: 0.5px;
  opacity: 0.55;
  text-transform: uppercase;
}

.dashboard {
  padding: 1.25rem 0 3rem;
}
.panel {
  background: rgba(255, 255, 255, 0.06);
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 18px;
  padding: 1rem 1.1rem 1.25rem;
  position: relative;
  backdrop-filter: blur(6px);
}
:root {
  --panel-bg: rgba(255, 255, 255, 0.07);
  --shadow-sm: 0 2px 8px -2px rgba(0, 0, 0, 0.4);
  --input-bg: rgba(255, 255, 255, 0.08);
  --border: rgba(255, 255, 255, 0.2);
  --focus: #6366f1;
}

.panel h3 {
  margin: 0.2rem 0 1rem;
  font-size: 0.85rem;
  letter-spacing: 0.5px;
  text-transform: uppercase;
  font-weight: 600;
  opacity: 0.9;
}
.game-wrapper {
  max-width: 960px;
  margin: 0 auto;
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
  padding: 0 1rem 2.5rem;
}
.game-panel {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1rem;
  text-align: center;
}
.wheel-wrapper {
  display: flex;
  justify-content: center;
  width: 100%;
}
.spin-meta {
  display: flex;
  gap: 0.5rem;
  flex-wrap: wrap;
  justify-content: center;
}
.pill {
  background: rgba(255, 255, 255, 0.08);
  padding: 0.25rem 0.6rem;
  border-radius: 20px;
  font-size: 0.65rem;
  letter-spacing: 0.5px;
}
.outcome-box {
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid rgba(255, 255, 255, 0.08);
  padding: 0.75rem 1rem;
  border-radius: 14px;
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  width: 100%;
  max-width: 420px;
}
.outcome-box .title {
  margin: 0;
  font-size: 0.7rem;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  opacity: 0.7;
}
.outcome-box .result {
  margin: 0;
  font-size: 0.9rem;
}
.outcome-box .balance {
  margin: 0;
  font-size: 0.75rem;
  opacity: 0.8;
}
.history-panel {
  max-height: 340px;
  overflow: auto;
}
.bet-history-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
}
.bet-history-list li {
  display: grid;
  grid-template-columns: 3.5rem 2.5rem 3.5rem 1fr;
  gap: 0.4rem;
  background: rgba(255, 255, 255, 0.05);
  padding: 0.45rem 0.6rem;
  border-radius: 8px;
  font-size: 0.7rem;
  align-items: center;
}
.bet-history-list li .id {
  opacity: 0.55;
}
.bet-history-list li .num {
  font-weight: 600;
}
.bet-history-list li .res {
  text-align: right;
  font-weight: 600;
}
.bet-history-list li .res.won {
  color: #4ade80;
}
.bet-history-list li .res.lost {
  color: #f87171;
}
.bet-history-list li.unsaved {
  opacity: 0.85;
  position: relative;
}
.bet-history-list li .res.pending {
  text-decoration: underline dotted;
}
.bet-history-list li .flag {
  font-size: 0.55rem;
  opacity: 0.6;
  margin-left: 0.4rem;
  letter-spacing: 0.5px;
  text-transform: uppercase;
}
.legend {
  margin-top: 0.5rem;
  opacity: 0.55;
  font-size: 0.6rem;
  letter-spacing: 0.5px;
}
.outcome p {
  margin: 0.3rem 0;
}
.outcome .won {
  color: #4ade80;
}
.outcome .lost {
  color: #f87171;
}
.details {
  opacity: 0.7;
  font-size: 0.75rem;
}

.loading-overlay {
  position: fixed;
  inset: 0;
  display: flex;
  justify-content: center;
  align-items: center;
  background: rgba(0, 0, 0, 0.55);
  font-size: 1.4rem;
  font-weight: 600;
  backdrop-filter: blur(4px);
}
.toast {
  position: fixed;
  bottom: 1rem;
  right: 1rem;
  background: #ef4444;
  color: #fff;
  padding: 0.75rem 1rem;
  border-radius: 10px;
  box-shadow: 0 8px 30px -8px rgba(0, 0, 0, 0.6);
}
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.3s;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

:root {
  --panel-bg: rgba(255, 255, 255, 0.07);
  --shadow-sm: 0 2px 8px -2px rgba(0, 0, 0, 0.4);
  --input-bg: rgba(255, 255, 255, 0.08);
  --border: rgba(255, 255, 255, 0.2);
  --focus: #6366f1;
}
</style>
