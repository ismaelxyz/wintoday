<script setup lang="ts">
import { reactive, computed, ref } from 'vue';
import { api } from './api';
import type { PlayerBalanceDto, SpinResultDto, BetOutcomeDto } from './types';
import SpinPanel from './components/SpinPanel.vue';
import RouletteWheel from './components/RouletteWheel.vue';
import BetForm from './components/BetForm.vue';

interface State {
  player: PlayerBalanceDto | null;
  lastSpin: SpinResultDto | null;
  lastBet: BetOutcomeDto | null;
  loading: boolean;
  error: string | null;
  pendingRoundId: string | null; // latest spin's round to bet on
  history: { spin: SpinResultDto; bet?: BetOutcomeDto }[];
}

const state = reactive<State>({
  player: null,
  lastSpin: null,
  lastBet: null,
  loading: false,
  error: null,
  pendingRoundId: null,
  history: [],
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

const wheelSpinning = ref(false);

async function ensureSpin(): Promise<SpinResultDto | null> {
  if (!state.player || wheelSpinning.value) return null;
  state.error = null;
  wheelSpinning.value = true;
  try {
    const result = await api.spin(state.player.name);
    state.lastSpin = result;
    state.pendingRoundId = result.roundId;
    state.history.unshift({ spin: result });
    return result;
  } catch (e: any) { state.error = e.message || 'Error'; wheelSpinning.value = false; return null; }
}

async function placeBet(payload: { wager: number; betType: string; color?: string | null; isEven?: boolean | null; number?: number | null }) {
  if (!state.player) return;
  state.error = null;
  if (!state.pendingRoundId) {
    const s = await ensureSpin();
    if (!s) return;
  }
  if (!state.pendingRoundId) return;
  state.loading = true;
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
    const idx = state.history.findIndex(h => h.spin.roundId === outcome.roundId);
    if (idx !== -1) state.history[idx].bet = outcome;
  } catch (e: any) { state.error = e.message || 'Error'; }
  finally { state.loading = false; }
}

function onWheelAnimationEnd() { wheelSpinning.value = false; }
const canBet = computed(() => !!state.player && !state.loading && !wheelSpinning.value);
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
      <div class="game-wrapper">
        <div class="panel game-panel">
          <h3>Ruleta & Apuesta</h3>
          <div class="wheel-wrapper">
            <RouletteWheel :spin="state.lastSpin" :spinning="wheelSpinning" @animation-end="onWheelAnimationEnd" />
          </div>
          <div class="spin-meta" v-if="state.lastSpin">
            <span class="pill num"># {{ state.lastSpin.number }}</span>
            <span class="pill col">{{ state.lastSpin.color }}</span>
            <span class="pill time">{{ new Date(state.lastSpin.createdAtUtc).toLocaleTimeString() }}</span>
          </div>
          <BetForm :round-id="state.pendingRoundId" :disabled="!canBet" @submit="placeBet" />
          <div class="outcome-box" v-if="state.lastBet">
            <p class="title">Última Apuesta</p>
            <p class="result" :class="state.lastBet.won ? 'won' : 'lost'">
              {{ state.lastBet.won ? '¡Ganaste!' : 'Perdiste' }} <strong>{{ state.lastBet.profit.toFixed(2) }}</strong>
            </p>
            <p class="balance">Balance: {{ state.player?.funds.toFixed(2) }}</p>
            <p class="details">Núm {{ state.lastBet.number }} | {{ state.lastBet.color }} | {{ state.lastBet.betType }}</p>
          </div>
        </div>
        <div class="panel history-panel">
          <h3>Historial de Apuestas</h3>
          <ul class="bet-history-list">
            <li v-for="h in state.history.filter(h=>h.bet)" :key="h.spin.roundId">
              <span class="id">{{ h.spin.roundId.slice(0,6) }}</span>
              <span class="num">{{ h.spin.number }}</span>
              <span class="colr">{{ h.spin.color }}</span>
              <span class="res" :class="h.bet!.won ? 'won' : 'lost'">{{ h.bet!.won ? '+'+h.bet!.profit.toFixed(2) : '-'+h.bet!.wager.toFixed(2) }}</span>
            </li>
          </ul>
        </div>
      </div>
    </section>

    <transition name="fade">
      <div v-if="state.loading && !wheelSpinning" class="loading-overlay">Cargando...</div>
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

.dashboard { padding:1.25rem 0 3rem; }
.panel { background:rgba(255,255,255,.06); border:1px solid rgba(255,255,255,.08); border-radius:18px; padding:1rem 1.1rem 1.25rem; position:relative; backdrop-filter: blur(6px); }
:root { --panel-bg: rgba(255,255,255,.07); --shadow-sm:0 2px 8px -2px rgba(0,0,0,.4); --input-bg: rgba(255,255,255,.08); --border: rgba(255,255,255,.2); --focus:#6366f1; }

.panel h3 { margin:.2rem 0 1rem; font-size:.85rem; letter-spacing:.5px; text-transform:uppercase; font-weight:600; opacity:.9; }
.game-wrapper { max-width:960px; margin:0 auto; display:flex; flex-direction:column; gap:1.5rem; padding:0 1rem 2.5rem; }
.game-panel { display:flex; flex-direction:column; align-items:center; gap:1rem; text-align:center; }
.wheel-wrapper { display:flex; justify-content:center; width:100%; }
.spin-meta { display:flex; gap:.5rem; flex-wrap:wrap; justify-content:center; }
.pill { background:rgba(255,255,255,.08); padding:.25rem .6rem; border-radius:20px; font-size:.65rem; letter-spacing:.5px; }
.outcome-box { background:rgba(255,255,255,.05); border:1px solid rgba(255,255,255,.08); padding:.75rem 1rem; border-radius:14px; display:flex; flex-direction:column; gap:.35rem; width:100%; max-width:420px; }
.outcome-box .title { margin:0; font-size:.7rem; text-transform:uppercase; letter-spacing:.5px; opacity:.7; }
.outcome-box .result { margin:0; font-size:.9rem; }
.outcome-box .balance { margin:0; font-size:.75rem; opacity:.8; }
.history-panel { max-height:340px; overflow:auto; }
.bet-history-list { list-style:none; margin:0; padding:0; display:flex; flex-direction:column; gap:.35rem; }
.bet-history-list li { display:grid; grid-template-columns: 3.5rem 2.5rem 3.5rem 1fr; gap:.4rem; background:rgba(255,255,255,.05); padding:.45rem .6rem; border-radius:8px; font-size:.7rem; align-items:center; }
.bet-history-list li .id { opacity:.55; }
.bet-history-list li .num { font-weight:600; }
.bet-history-list li .res { text-align:right; font-weight:600; }
.bet-history-list li .res.won { color:#4ade80; }
.bet-history-list li .res.lost { color:#f87171; }
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
