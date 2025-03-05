import * as signalR from '@microsoft/signalr';

function extractArray(data: any): any[] {
    if (!data) return [];
    if (data.$values) return data.$values;
    if (Array.isArray(data)) return data;
    return [];
}

class SignalRService {
    private connection: signalR.HubConnection;
    private connectionPromise: Promise<void> | null = null;

    constructor() {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl('http://localhost:5223/gamehub')
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Information)
            .build();

        // Add connection event listeners
        this.connection.onclose(() => {
            console.log('SignalR connection closed');
        });

        this.connection.onreconnecting(() => {
            console.log('SignalR reconnecting...');
        });

        this.connection.onreconnected(() => {
            console.log('SignalR reconnected');
        });
    }

    async connect(): Promise<void> {
        if (!this.connectionPromise) {
            this.connectionPromise = this.connection.start();

            try {
                await this.connectionPromise;
                console.log('SignalR connected');
            } catch (err) {
                console.error('SignalR connection error:', err);
                this.connectionPromise = null;
                throw err;
            }
        }

        return this.connectionPromise;
    }

    // Host methods
    async registerAsHost() {
        await this.connect();

        // Add explicit error handling and retry logic
        try {
            await this.connection.invoke('RegisterAsHost');
            console.log("Successfully registered as host");
            return true;
        } catch (error) {
            console.error("Error registering as host:", error);

            // Try reconnecting and registering again
            await this.connection.stop();
            this.connectionPromise = null;
            await this.connect();
            await this.connection.invoke('RegisterAsHost');
            return true;
        }
    }

    async createGame(name: string) {
        await this.connect();
        return await this.connection.invoke('CreateGame', name);
    }

    async getGames() {
        await this.connect();
        try {
            console.log("Calling GetGames hub method...");
            const result = await this.connection.invoke('GetGames');
            console.log("Raw result from GetGames:", result);
            const games = extractArray(result);
            console.log("Processed games array:", games);
            return games;
        } catch (error) {
            console.error("Error getting games:", error);
            return [];
        }
    }

    async addQuestion(gameId: string, questionText: string, options: {text: string, isCorrect: boolean}[]) {
        await this.connect();
        return await this.connection.invoke('AddQuestion', gameId, questionText, options);
    }

    async startGame(gameId: string) {
        await this.connect();
        await this.connection.invoke('StartGame', gameId);
    }

    async startQuestion(gameId: string, questionId: string) {
        await this.connect();
        await this.connection.invoke('StartQuestion', gameId, questionId);
    }

    async endQuestion(gameId: string, questionId: string) {
        await this.connect();
        await this.connection.invoke('EndQuestion', gameId, questionId);
    }

    async getScores(gameId: string) {
        await this.connect();
        return await this.connection.invoke('GetScores', gameId);
    }

    // Player methods
    async joinGame(gameId: string, nickname: string) {
        await this.connect();
        await this.connection.invoke('JoinGame', gameId, nickname);
    }

    async submitAnswer(questionId: string, optionId: string) {
        await this.connect();
        await this.connection.invoke('SubmitAnswer', questionId, optionId);
    }
    
    async getGameById(gameId: string) {
        await this.connect();
        try {
            const result = await this.connection.invoke('GetGameById', gameId);
            console.log("Raw game data:", result);

            // Process nested arrays in the result
            if (result) {
                if (result.questions && result.questions.$values) {
                    result.questions = result.questions.$values;
                }
                if (result.players && result.players.$values) {
                    result.players = result.players.$values;
                }

                // Process question options as well
                if (result.questions) {
                    result.questions.forEach((q: any) => {
                        if (q.options && q.options.$values) {
                            q.options = q.options.$values;
                        }
                    });
                }
            }

            return result;
        } catch (error) {
            console.error("Error getting game details:", error);
            return null;
        }
    }

    // Events
    onPlayerJoined(callback: (nickname: string) => void) {
        this.connection.on('PlayerJoined', callback);  // Changed from 'playerjoined'
    }

    onGameStarted(callback: () => void) {
        this.connection.on('GameStarted', callback);  // Changed from 'gamestarted'
    }

    onQuestionStarted(callback: (question: any) => void) {
        this.connection.on('QuestionStarted', callback);  // Changed from 'questionstarted'
    }

    onQuestionEnded(callback: (questionId: string) => void) {
        this.connection.on('QuestionEnded', callback);  // Changed from 'questionended'
    }

    onScoresUpdated(callback: (scores: Record<string, number>) => void) {
        this.connection.on('ScoresUpdated', callback);  // Changed from 'scoresupdated'
    }

    onPlayerLeft(callback: (playerId: string) => void) {
        this.connection.on('PlayerLeft', callback);  // Changed from 'playerleft'
    }

    onHostRegistered(callback: () => void) {
        this.connection.on('HostRegistered', callback);  // Changed from 'hostregistered'
    }
}

export default new SignalRService();