// Shared DTO & request type definitions matching backend
export interface PlayerBalanceDto {
    name: string;
    funds: number;
}

export interface SpinResultDto {
    roundId: string;
    number: number;
    color: string; // RouletteColor enum string
    createdAtUtc: string;
}

export interface BetOutcomeDto {
    roundId: string;
    betId: string;
    number: number;
    color: string;
    wager: number;
    profit: number;
    newBalance: number;
    won: boolean;
    betType: string;
    criteria: unknown;
}

export interface BetHistoryItemDto {
    roundId: string;
    betId: string;
    number: number;
    color: string;
    roundCreatedAtUtc: string;
    wager: number;
    profit: number;
    won: boolean;
    betType: string;
    criteria: unknown;
}

// Session (frontend batched play)
export interface SaveSessionBetItem {
    wager: number;
    betType: string; // color | colorParity | exact
    color?: string | null;
    isEven?: boolean | null;
    number?: number | null;
    numberResult: number; // spin outcome number
    colorResult: string;  // spin outcome color
    playedAtUtc?: string | null;
}

export interface SaveSessionRequest {
    playerName: string;
    bets: SaveSessionBetItem[];
}

export interface SessionSaveResultDto {
    startingFunds: number;
    endingFunds: number;
    outcomes: BetOutcomeDto[]; // outcomes as evaluated by server
}

export interface LoginRequest { playerName: string }

export interface CommitBetRequest {
    roundId: string;
    playerName: string;
    wager: number;
    betType: string; // color | colorParity | exact
    color?: string | null;
    isEven?: boolean | null;
    number?: number | null;
}

export type ApiError = { message: string; status?: number };
