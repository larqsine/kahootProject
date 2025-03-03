import React, { useEffect, useState } from 'react';
import { useWebSocket } from '../WebSocketContext';
import type { Question, QuestionResult, LeaderboardEntry } from '../types';

export const Game: React.FC = () => {
    const { connected } = useWebSocket();
    const { sendMessage, lastMessage } = useWebSocket();
    const [gameId, setGameId] = useState('');
    const [nickname, setNickname] = useState('');
    const [isHost, setIsHost] = useState(false);
    const [isJoined, setIsJoined] = useState(false);
    const [currentQuestion, setCurrentQuestion] = useState<Question | null>(null);
    const [results, setResults] = useState<QuestionResult[] | null>(null);
    const [leaderboard, setLeaderboard] = useState<LeaderboardEntry[] | null>(null);
    const [gameStarted, setGameStarted] = useState(false);

    useEffect(() => {
        if (!lastMessage) return;
        console.log('Received message:', lastMessage);

        switch (lastMessage.type) {
            case 'game-created':
                setGameId(lastMessage.gameId!);
                setIsJoined(true);
                break;
            case 'player-joined':
                setIsJoined(true);
                break;
            case 'game-started':
            case 'new-question':
                setCurrentQuestion(lastMessage.question!);
                setResults(null);
                setGameStarted(true);
                break;
            case 'question-results':
                setResults(lastMessage.results!);
                break;
            case 'game-over':
                setLeaderboard(lastMessage.leaderboard!);
                setGameStarted(false);
                break;
        }
    }, [lastMessage]);

    const createGame = () => {
        setIsHost(true);
        sendMessage({
            type: 'host-create-game',
            name: 'New Game'
        });
    };

    const joinGame = () => {
        if (!gameId || !nickname) {
            alert('Please enter both Game ID and Nickname');
            return;
        }
        sendMessage({
            type: 'player-join',
            gameId,
            nickname
        });
    };

    const startGame = () => {
        sendMessage({
            type: 'start-game',
            gameId
        });
    };

    const submitAnswer = (optionIndex: number) => {
        sendMessage({
            type: 'submit-answer',
            gameId,
            questionId: 'Q1',
            optionIndex
        });
    };

    const nextQuestion = () => {
        sendMessage({
            type: 'next-question',
            gameId
        });
    };

    return (
        <div className="game-container">
            <h1>Kahoot-like Game</h1>
            {!connected && (
                <div style={{ color: 'red', marginBottom: '1rem' }}>
                    Not connected to server
                </div>
            )}
            {/* Initial setup - not joined and not started */}
            {!isJoined && !gameStarted && (
                <div className="setup-controls">
                    <button onClick={createGame}>Create New Game</button>
                    <div>
                        <input
                            type="text"
                            placeholder="Game ID"
                            value={gameId}
                            onChange={(e) => setGameId(e.target.value)}
                        />
                        <input
                            type="text"
                            placeholder="Nickname"
                            value={nickname}
                            onChange={(e) => setNickname(e.target.value)}
                        />
                        <button onClick={joinGame}>Join Game</button>
                    </div>
                </div>
            )}

            {/* Waiting room - joined but not started */}
            {isJoined && !gameStarted && (
                <div className="waiting-room">
                    <h2>Game ID: {gameId}</h2>
                    {isHost ? (
                        <div className="host-controls">
                            <p>Waiting for players to join...</p>
                            <button onClick={startGame}>Start Game</button>
                        </div>
                    ) : (
                        <p>Waiting for host to start the game...</p>
                    )}
                </div>
            )}

            {/* Game in progress */}
            {gameStarted && currentQuestion && (
                <div className="question-container">
                    <h2>{currentQuestion.text}</h2>
                    <div className="options">
                        {currentQuestion.options.map((option, index) => (
                            <button
                                key={index}
                                onClick={() => submitAnswer(index)}
                                disabled={results !== null}
                            >
                                {option}
                            </button>
                        ))}
                    </div>
                </div>
            )}

            {/* Results display */}
            {results && (
                <div className="results">
                    <h3>Results:</h3>
                    {results.map((result, index) => (
                        <div key={index}>
                            Player {result.playerId}: {result.correct ? '✅' : '❌'} ({result.time.toFixed(2)}s)
                        </div>
                    ))}
                    {isHost && <button onClick={nextQuestion}>Next Question</button>}
                </div>
            )}

            {/* Leaderboard */}
            {leaderboard && (
                <div className="leaderboard">
                    <h2>Final Scores</h2>
                    {leaderboard.map((entry, index) => (
                        <div key={index}>
                            {index + 1}. {entry.nickname}: {entry.score} points
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
};