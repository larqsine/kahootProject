import {WsClientProvider} from "ws-request-hook";
import {Toaster} from "react-hot-toast";

import Lobby from "./Lobby.tsx";

export default function App() {
    return (
        <WsClientProvider url={'wss://kahoot-267099996159.europe-north1.run.app?id=' + crypto.randomUUID()}>
            <Toaster />
            <Lobby />
        </WsClientProvider>

    )
}