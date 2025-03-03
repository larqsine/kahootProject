import React, { useState, useEffect } from 'react';
import signalRService from '../services/signalRService';

const GamePlayer: React.FC = () => {
    const [gameId, setGameId] = useState('');
    const [nickname, setNickname] = useState('');
    const [joined, setJoined] = useState(false);
    const [gameStarted, setGameStarted] = useState(false);
    const [currentQuestion, setCurrentQuestion] = useState<any>(null);
    const [selectedOption, setSelectedOption] = useState('');
    const [answered, setAnswered] = useState(false);
    const [scores, setScores] = useState<Record<string, number>>({});

    useEffect(() => {
        signalRService.onGameStarted(() => {
            setGameStarted(true);
        });

        signalRService.onQuestionStarted((question) => {
            setCurrentQuestion(question);
            setSelectedOption('');
            setAnswered(false);
        });

        signalRService.onQuestionEnded(() => {
            setCurrentQuestion(null);
        });

        signalRService.onScoresUpdated((updatedScores) => {
            setScores(updatedScores);
        });

        return () => {
            // Clean up event handlers if needed
        };
    }, []);

    const joinGame = async () => {
        if (!gameId || !nickname) return;

        try {
            await signalRService.joinGame(gameId, nickname);
            setJoined(true);
        } catch (error) {
            console.error('Failed to join game:', error);
        }
    };

    const submitAnswer = async (optionId: string) => {
        if (!currentQuestion) return;

        try {
            await signalRService.submitAnswer(currentQuestion.id, optionId);
            setSelectedOption(optionId);
            setAnswered(true);
        } catch (error) {
            console.error('Failed to submit answer:', error);
        }
    };

    return (
        <div className="container mt-4">
            <h2>Game Player</h2>

            {!joined ? (
                <div className="card p-3">
                    <h3>Join Game</h3>
                    <div className="mb-3">
                        <label className="form-label">Game ID</label>
                        <input
                            type="text"
                            className="form-control"
                            value={gameId}
                            onChange={(e) => setGameId(e.target.value)}
                        />
                    </div>
                    <div className="mb-3">
                        <label className="form-label">Your Nickname</label>
                        <input
                            type="text"
                            className="form-control"
                            value={nickname}
                            onChange={(e) => setNickname(e.target.value)}
                        />
                    </div>
                    <button className="btn btn-primary" onClick={joinGame}>
                        Join
                    </button>
                </div>
            ) : (
                <div>
                    <div className="card mb-4 p-3">
                        <h3>Game: {gameId}</h3>
                        <p>Player: {nickname}</p>

                        {!gameStarted ? (
                            <div className="alert alert-info">
                                Waiting for the host to start the game...
                            </div>
                        ) : !currentQuestion ? (
                            <div className="alert alert-info">
                                Waiting for the next question...
                            </div>
                        ) : (
                            <div className="card p-3">
                                <h4>{currentQuestion.text}</h4>
                                <div className="list-group">
                                    {currentQuestion.options.map((option: any) => (
                                        <button
                                            key={option.id}
                                            className={`list-group-item list-group-item-action ${selectedOption === option.id ? 'active' : ''}`}
                                            disabled={answered}
                                            onClick={() => submitAnswer(option.id)}
                                        >
                                            {option.text}
                                        </button>
                                    ))}
                                </div>
                                {answered && (
                                    <div className="alert alert-success mt-3">
                                        Answer submitted!
                                    </div>
                                )}
                            </div>
                        )}
                    </div>

                    {Object.keys(scores).length > 0 && (
                        <div className="card p-3">
                            <h3>Scoreboard</h3>
                            <ul className="list-group">
                                {Object.entries(scores)
                                    .sort(([, a], [, b]) => b - a)
                                    .map(([player, score]) => (
                                        <li key={player} className="list-group-item d-flex justify-content-between align-items-center">
                                            {player}
                                            <span className="badge bg-primary rounded-pill">{score}</span>
                                        </li>
                                    ))}
                            </ul>
                        </div>
                    )}
                </div>
            )}
        </div>
    );
};

export default GamePlayer;