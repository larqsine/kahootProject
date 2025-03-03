import { useState } from 'react';
import 'bootstrap/dist/css/bootstrap.min.css';
import GameHost from './components/GameHost';
import GamePlayer from './components/GamePlayer';

function App() {
    const [role, setRole] = useState<'host' | 'player' | null>(null);

    return (
        <div className="container mt-4">
            <h1>Kahoot Clone</h1>

            {!role ? (
                <div className="row mt-5">
                    <div className="col-md-6">
                        <div className="card p-4 text-center">
                            <h3>Choose Your Role</h3>
                            <button
                                className="btn btn-primary btn-lg mt-3"
                                onClick={() => setRole('host')}
                            >
                                Game Host
                            </button>
                            <button
                                className="btn btn-success btn-lg mt-3"
                                onClick={() => setRole('player')}
                            >
                                Player
                            </button>
                        </div>
                    </div>
                </div>
            ) : role === 'host' ? (
                <GameHost />
            ) : (
                <GamePlayer />
            )}
        </div>
    );
}

export default App;