<script setup lang="ts">
import { reactive, computed } from 'vue';
import { api } from './api';
import type { PlayerBalanceDto, SpinResultDto, BetOutcomeDto } from './types';
import SpinPanel from './components/SpinPanel.vue';
import BetForm from './components/BetForm.vue';

interface State {
  player: PlayerBalanceDto | null;
  lastSpin: SpinResultDto | null;
  lastBet: BetOutcomeDto | null;
  loading: boolean;
  error: string | null;
  pendingRoundId: string | null; // latest spin's round to bet on
}

const state = reactive<State>({
  player: null,
  lastSpin: null,
  lastBet: null,
  loading: false,
  error: null,
  pendingRoundId: null,
});

async function login(name: string) {
  state.error = null; state.loading = true;
  try { state.player = await api.login({ playerName: name.trim() }); }
  catch (e: any) { state.error = e.message || 'Error'; }
  finally { state.loading = false; }
}

async function refreshPlayer() {
  if (!state.player) return; try { state.player = await api.getPlayer(state.player.name); } catch {}
}

async function spin() {
  if (!state.player) return;
  state.loading = true; state.error = null;
  try {
    const result = await api.spin(state.player.name);
    state.lastSpin = result;
    state.pendingRoundId = result.roundId;
  } catch (e: any) { state.error = e.message || 'Error'; }
  finally { state.loading = false; }
}

async function placeBet(payload: { wager: number; betType: string; color?: string | null; isEven?: boolean | null; number?: number | null }) {
  if (!state.player || !state.pendingRoundId) return;
  state.loading = true; state.error = null;
  try {
    const outcome = await api.commitBet({
      roundId: state.pendingRoundId,
      playerName: state.player.name,
      wager: payload.wager,
      betType: payload.betType,
      color: payload.color ?? undefined,
      isEven: payload.isEven ?? undefined,
      number: payload.number ?? undefined,
    });
    state.lastBet = outcome;
    state.player.funds = outcome.newBalance;
    await refreshPlayer();
  } catch (e: any) { state.error = e.message || 'Error'; }
  finally { state.loading = false; }
}

const canSpin = computed(() => !!state.player && !state.loading);
const loginName = reactive({ value: '' });
</script>

<template>
  <main class="app-root">
    <header class="site-header" v-if="state.player">
      <div class="brand">WinToday <span class="tag">Demo</span></div>
      <div class="player-box" v-if="state.player">
        <span class="player-name">👤 {{ state.player.name }}</span>
        <span class="funds">💰 {{ state.player.funds.toFixed(2) }}</span>
      </div>
    </header>

    <section v-if="!state.player" class="landing">
      <div class="hero">
        <h1>WinToday Roulette</h1>
        <p class="tagline">Gira la ruleta, apuesta y mira si la suerte está contigo.</p>
        <form @submit.prevent="login(loginName.value)" class="login-form">
          <input v-model.trim="loginName.value" placeholder="Tu nombre" required :disabled="state.loading" />
          <button class="primary" :disabled="!loginName.value || state.loading">Entrar</button>
        </form>
        <p class="hint">Se crea tu jugador si no existe.</p>
      </div>
    </section>

    <section v-else class="dashboard">
      <div class="container">
        <div class="actions">
          <button class="spin-btn" :disabled="!canSpin" @click="spin">🎲 Girar</button>
          <small class="muted">(Gira y luego apuesta a la misma ronda - flujo simplificado)</small>
        </div>
        <div class="grid game-layout">
          <SpinPanel class="spin-panel" :spin="state.lastSpin" />
          <BetForm class="bet-panel" :round-id="state.pendingRoundId" :disabled="state.loading || !state.pendingRoundId" @submit="placeBet" />
          <div class="panel outcome outcome-panel" v-if="state.lastBet">
            <h3>Resultado Apuesta</h3>
              <p :class="state.lastBet.won ? 'won' : 'lost'">
                {{ state.lastBet.won ? '¡Ganaste!' : 'Perdiste' }}
                <strong>{{ state.lastBet.profit.toFixed(2) }}</strong>
              </p>
              <p>Balance: {{ state.lastBet.newBalance.toFixed(2) }}</p>
              <p class="details">Número: {{ state.lastBet.number }} | Color: {{ state.lastBet.color }}</p>
          </div>
        </div>
      </div>
    </section>

    <transition name="fade">
      <div v-if="state.loading" class="loading-overlay">Cargando...</div>
    </transition>
    <transition name="fade">
      <div v-if="state.error" class="toast error">{{ state.error }}</div>
    </transition>
  </main>
</template>

<style scoped>
.app-root { min-height: 100dvh; background: radial-gradient(circle at 30% 20%, #1e1b4b, #020617); color: #f1f5f9; font-family: system-ui, 'Segoe UI', Roboto, sans-serif; }
.site-header { display:flex; justify-content: space-between; align-items:center; padding: .85rem 1.25rem; background: rgba(0,0,0,.25); backdrop-filter: blur(8px); position: sticky; top:0; z-index:10; }
.brand { font-weight:700; font-size:1.1rem; letter-spacing:.5px; }
.brand .tag { font-size:.65rem; margin-left:.4rem; background:#6366f1; padding:.2rem .4rem; border-radius:4px; text-transform:uppercase; }
.player-box { display:flex; gap:1rem; font-size:.9rem; }
.funds { font-weight:600; }
.landing { display:grid; place-items:center; padding:4rem 1rem 2rem; }
.hero { max-width:560px; text-align:center; display:flex; flex-direction:column; gap:1.2rem; background:rgba(255,255,255,.04); padding:2.5rem 2rem 3rem; border-radius:28px; box-shadow:0 10px 35px -10px rgba(0,0,0,.6), inset 0 0 0 1px rgba(255,255,255,.05); position:relative; }
h1 { font-size: clamp(2.2rem, 5.5vw, 3.3rem); background: linear-gradient(90deg,#818cf8,#c084fc); -webkit-background-clip:text; color:transparent; margin:0; }
.tagline { margin:0; font-size:1.05rem; line-height:1.3; opacity:.85; }
.login-form { display:flex; gap:.75rem; justify-content:center; }
input { background:rgba(255,255,255,.1); border:1px solid rgba(255,255,255,.2); padding:.85rem 1rem; border-radius:14px; color:#fff; font-size:.95rem; width:55%; min-width:180px; }
input:focus { outline:2px solid #6366f1; outline-offset:2px; }
button.primary { background:linear-gradient(90deg,#6366f1,#8b5cf6); border:none; color:#fff; padding:.9rem 1.4rem; border-radius:14px; font-weight:600; cursor:pointer; box-shadow:0 4px 18px -4px rgba(99,102,241,.7); }
button.primary:disabled { opacity:.5; cursor:not-allowed; }
.hint { font-size:.7rem; letter-spacing:.5px; opacity:.55; text-transform:uppercase; }

.dashboard { padding:1.25rem 0 3rem; display:flex; flex-direction:column; gap:1.25rem; }
.container { width:100%; max-width:100%; margin:0; padding:0 clamp(.9rem,1.8vw,2.4rem); }
.actions { display:flex; flex-direction:column; gap:.35rem; align-items:flex-start; }
.spin-btn { background:linear-gradient(135deg,#0ea5e9,#6366f1); border:none; color:#fff; padding:.9rem 1.5rem; font-size:1.05rem; font-weight:600; border-radius:14px; cursor:pointer; box-shadow:0 6px 25px -6px rgba(14,165,233,.6); }
.spin-btn:disabled { opacity:.5; cursor:not-allowed; }
.muted { opacity:.55; font-size:.7rem; }
.grid { display:grid; gap:1.25rem; align-items:start; width:100%; }
.game-layout { grid-template-columns: repeat(auto-fit,minmax(280px,1fr)); }
.panel { background:rgba(255,255,255,.06); border:1px solid rgba(255,255,255,.08); border-radius:18px; padding:1rem 1.1rem 1.25rem; position:relative; backdrop-filter: blur(6px); }
:root { --panel-bg: rgba(255,255,255,.07); --shadow-sm:0 2px 8px -2px rgba(0,0,0,.4); --input-bg: rgba(255,255,255,.08); --border: rgba(255,255,255,.2); --focus:#6366f1; }

/* Desktop Responsive Layout */
@media (min-width: 880px) {
  /* Allow panels to stretch with screen; introduce third column space if very wide */
  .game-layout { grid-template-columns: repeat(auto-fit,minmax(340px,1fr)); grid-auto-rows: minmax(180px, auto); }
  .spin-panel { grid-area: spin; }
  .bet-panel { grid-area: bet; }
  .outcome-panel { grid-area: outcome; }
  .actions { flex-direction:row; align-items:center; justify-content:space-between; }
}

@media (min-width: 1180px) {
  .game-layout { grid-template-columns: repeat(auto-fit,minmax(360px,1fr)); }
  h1 { letter-spacing: -.5px; }
}

/* Larger screens: tighten panel spacing slightly*/
@media (min-width:1600px) {
  .grid { gap:1.5rem; }
}
.panel h3 { margin:.2rem 0 1rem; font-size:1rem; letter-spacing:.5px; text-transform:uppercase; font-weight:600; opacity:.9; }
.outcome p { margin:.3rem 0; }
.outcome .won { color:#4ade80; }
.outcome .lost { color:#f87171; }
.details { opacity:.7; font-size:.75rem; }

.loading-overlay { position:fixed; inset:0; display:flex; justify-content:center; align-items:center; background:rgba(0,0,0,.55); font-size:1.4rem; font-weight:600; backdrop-filter:blur(4px); }
.toast { position:fixed; bottom:1rem; right:1rem; background:#ef4444; color:#fff; padding:.75rem 1rem; border-radius:10px; box-shadow:0 8px 30px -8px rgba(0,0,0,.6); }
.fade-enter-active,.fade-leave-active { transition: opacity .3s; }
.fade-enter-from,.fade-leave-to { opacity:0; }

:root { --panel-bg: rgba(255,255,255,.07); --shadow-sm:0 2px 8px -2px rgba(0,0,0,.4); --input-bg: rgba(255,255,255,.08); --border: rgba(255,255,255,.2); --focus:#6366f1; }
</style>
