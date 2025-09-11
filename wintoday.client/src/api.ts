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
        return request<SpinResultDto>(`/api/Game/spin/${encodeURIComponent(playerName)}`, { method: 'POST' });
    },
    commitBet(data: CommitBetRequest) {
        return request<BetOutcomeDto>('/api/Game/commit-bet', { method: 'POST', body: JSON.stringify(data) });
    }
};
