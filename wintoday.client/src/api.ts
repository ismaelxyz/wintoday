import type { PlayerBalanceDto, SpinResultDto, BetOutcomeDto, LoginRequest, CommitBetRequest, ApiError } from './types';

const base = import.meta.env.VITE_API_BASE?.replace(/\/$/, '') || '';

async function request<T>(url: string, options?: RequestInit): Promise<T> {
    const res = await fetch(base + url, {
        headers: { 'Content-Type': 'application/json', ...(options?.headers || {}) },
        ...options,
    });
    if (!res.ok) {
        let message = res.statusText;
        try {
            const body = await res.json();
            if (body?.message) message = body.message;
        } catch { /* ignore */ }
        throw <ApiError>{ message, status: res.status };
    }
    if (res.status === 204) return undefined as unknown as T;
    return res.json() as Promise<T>;
}

export const api = {
    login(data: LoginRequest) {
        return request<PlayerBalanceDto>('/api/Players/login', { method: 'POST', body: JSON.stringify(data) });
    },
    getPlayer(name: string) {
        return request<PlayerBalanceDto>(`/api/Players/${encodeURIComponent(name)}`);
    },
    spin(playerName: string) {
        return request<unknown>(`/api/Game/spin/${encodeURIComponent(playerName)}`, { method: 'POST' })
            .then(r => normalizeSpin(r));
    },
    commitBet(data: CommitBetRequest) {
        return request<unknown>('/api/Game/commit-bet', { method: 'POST', body: JSON.stringify(data) })
            .then(r => normalizeBetOutcome(r));
    }
};

function normColor(c: unknown): string {
    if (c == null) return '';
    if (typeof c === 'string') return c;
    switch (c) {
        case 1: return 'Red';
        case 2: return 'Black';
        case 3: return 'Green';
        default: return String(c);
    }
}

function normalizeSpin(r: unknown): SpinResultDto {
    if (typeof r !== 'object' || r === null) throw new Error('Invalid spin payload');
    const obj = r as Record<string, unknown>;
    return {
        roundId: String(obj.roundId),
        number: Number(obj.number),
        color: normColor(obj.color),
        createdAtUtc: String(obj.createdAtUtc)
    };
}
function normalizeBetOutcome(r: unknown): BetOutcomeDto {
    if (typeof r !== 'object' || r === null) throw new Error('Invalid bet outcome payload');
    const obj = r as Record<string, unknown>;
    return {
        roundId: String(obj.roundId),
        betId: String(obj.betId),
        number: Number(obj.number),
        color: normColor(obj.color),
        wager: Number(obj.wager),
        profit: Number(obj.profit),
        newBalance: Number(obj.newBalance),
        won: Boolean(obj.won),
        betType: String(obj.betType),
        criteria: obj.criteria
    };
}
