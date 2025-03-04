import React, { useState, useEffect } from 'react';
import signalRService from '../services/signalRService';
import QuestionForm from './QuestionForm';
import ScoreBoard from './ScoreBoard';

const GameHost: React.FC = () => {
    const [isHost, setIsHost] = useState(false);
    const [gameName, setGameName] = useState('');
    const [gameId, setGameId] = useState('');
    const [games, setGames] = useState<any[]>([]);
    const [currentGame, setCurrentGame] = useState<any>(null);
    const [players, setPlayers] = useState<string[]>([]);
    const [gameStarted, setGameStarted] = useState(false);
    const [currentQuestionId, setCurrentQuestionId] = useState('');
    const [scores, setScores] = useState<Record<string, number>>({});

    useEffect(() => {
        const registerAsHost = async () => {
            try {
                await signalRService.registerAsHost();
                setIsHost(true);
                loadGames();
            } catch (error) {
                console.error('Failed to register as host:', error);
            }
        };

        registerAsHost();

        signalRService.onPlayerJoined((nickname) => {
            setPlayers(prev => [...prev, nickname]);
            console.log(`Player joined: ${nickname}`);
        });

        signalRService.onPlayerLeft((playerId) => {
            console.log(`Player left: ${playerId}`);
        });

        signalRService.onScoresUpdated((updatedScores) => {
            setScores(updatedScores);
        });

        return () => {
            // Clean up event handlers if needed
        };
    }, []);

    const loadGames = async () => {
        try {
            const gameList = await signalRService.getGames();
            // Make sure we always set an array
            setGames(Array.isArray(gameList) ? gameList : []);
            console.log("Games loaded:", gameList); // Add this for debugging
        } catch (error) {
            console.error('Failed to load games:', error);
            setGames([]);
        }
    };

    const createGame = async () => {
        if (!gameName) return;

        try {
            const id = await signalRService.createGame(gameName);
            setGameId(id);
            loadGames();

            // Load game details
            const gameDetails = await signalRService.getGameById(id);
            setCurrentGame(gameDetails);
        } catch (error) {
            console.error('Failed to create game:', error);
        }
    };

    const addQuestion = async (questionText: string, options: {text: string, isCorrect: boolean}[]) => {
        if (!gameId) return;

        try {
            // Re-register as host before adding question
            await signalRService.registerAsHost();

            await signalRService.addQuestion(gameId, questionText, options);

            // Refresh game details
            const gameDetails = await signalRService.getGameById(gameId);
            setCurrentGame(gameDetails);
        } catch (error) {
            console.error('Failed to add question:', error);
        }
    };

    const startGame = async () => {
        if (!gameId) return;

        try {
            await signalRService.startGame(gameId);
            setGameStarted(true);
        } catch (error) {
            console.error('Failed to start game:', error);
        }
    };

    const startQuestion = async (questionId: string) => {
        if (!gameId) return;

        try {
            await signalRService.startQuestion(gameId, questionId);
            setCurrentQuestionId(questionId);
        } catch (error) {
            console.error('Failed to start question:', error);
        }
    };

    const endQuestion = async () => {
        if (!gameId || !currentQuestionId) return;

        try {
            await signalRService.endQuestion(gameId, currentQuestionId);
            setCurrentQuestionId('');

            // Get scores
            const gameScores = await signalRService.getScores(gameId);
            setScores(gameScores);
        } catch (error) {
            console.error('Failed to end question:', error);
        }
    };

    return (
        <div className="container mt-4">
            <h2>Game Host</h2>

            {!isHost ? (
                <div className="alert alert-info">Connecting as host...</div>
            ) : (
                <div>
                    {!gameId ? (
                        <div className="card mb-4 p-3">
                            <h3>Create New Game</h3>
                            <div className="input-group mb-3">
                                <input
                                    type="text"
                                    className="form-control"
                                    placeholder="Game Name"
                                    value={gameName}
                                    onChange={(e) => setGameName(e.target.value)}
                                />
                                <button className="btn btn-primary" onClick={createGame}>
                                    Create
                                </button>
                            </div>

                            <h3>Existing Games</h3>
                            <ul className="list-group">
                                {Array.isArray(games) ? (
                                    games.map(game => (
                                        <li key={game.id} className="list-group-item">
                                            {game.name}
                                        </li>
                                    ))
                                ) : (
                                    <li className="list-group-item">No games available</li>
                                )}
                            </ul>
                        </div>
                    ) : (
                        <div>
                            <div className="card mb-4 p-3">
                                <h3>Game: {currentGame?.name}</h3>
                                <p>Game ID: {gameId}</p>

                                <h4>Players ({players.length})</h4>
                                <ul className="list-group mb-3">
                                    {players.map((player, index) => (
                                        <li key={index} className="list-group-item">
                                            {player}
                                        </li>
                                    ))}
                                </ul>

                                {!gameStarted && (
                                    <button className="btn btn-success" onClick={startGame}>
                                        Start Game
                                    </button>
                                )}
                            </div>

                            {!gameStarted && (
                                <QuestionForm onSubmit={addQuestion} />
                            )}

                            {gameStarted && (
                                <div className="card mb-4 p-3">
                                    <h3>Questions</h3>
                                    <ul className="list-group mb-3">
                                        {currentGame?.questions?.map((q: any) => (
                                            <li key={q.id} className="list-group-item">
                                                {q.questionText}
                                                {currentQuestionId === q.id ? (
                                                    <button
                                                        className="btn btn-warning ms-2"
                                                        onClick={() => endQuestion()}
                                                    >
                                                        End Question
                                                    </button>
                                                ) : !q.answered && (
                                                    <button
                                                        className="btn btn-primary ms-2"
                                                        onClick={() => startQuestion(q.id)}
                                                    >
                                                        Start Question
                                                    </button>
                                                )}
                                            </li>
                                        ))}
                                    </ul>

                                    <ScoreBoard scores={scores} />
                                </div>
                            )}
                        </div>
                    )}
                </div>
            )}
        </div>
    );
};

export default GameHost;