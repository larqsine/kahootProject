import React from 'react';

interface ScoreBoardProps {
    scores: Record<string, number>;
}

const ScoreBoard: React.FC<ScoreBoardProps> = ({ scores }) => {
    const sortedScores = Object.entries(scores)
        .sort(([, a], [, b]) => b - a);

    if (sortedScores.length === 0) {
        return null;
    }

    return (
        <div>
            <h3>Scores</h3>
            <ul className="list-group">
                {sortedScores.map(([player, score], index) => (
                    <li key={player} className="list-group-item d-flex justify-content-between align-items-center">
                        {index + 1}. {player}
                        <span className="badge bg-primary rounded-pill">{score}</span>
                    </li>
                ))}
            </ul>
        </div>
    );
};

export default ScoreBoard;