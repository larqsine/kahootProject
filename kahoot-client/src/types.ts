export interface Game {
    id: string;
    name: string;
}

export interface Question {
    text: string;
    options: string[];
    timeLimit: number;
}

export interface Player {
    id: string;
    nickname: string;
}

export interface QuestionResult {
    playerId: string;
    correct: boolean;
    time: number;
}

export interface LeaderboardEntry {
    nickname: string;
    score: number;
}

export interface WebSocketMessage {
    type: string;
    gameId?: string;
    name?: string;
    player?: Player;
    question?: Question;
    results?: QuestionResult[];
    correctOptionIndex?: number;
    leaderboard?: LeaderboardEntry[];
    message?: string;
}