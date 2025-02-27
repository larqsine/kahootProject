import {WsClientProvider} from "ws-request-hook";
import {Toaster} from "react-hot-toast";
import Lobby from "./Lobby.tsx";
const baseUrl = import.meta.env.VITE_API_BASE_URL

export default function App() {
    return (
        <WsClientProvider url={baseUrl+'?id=' + crypto.randomUUID()}>
            <Toaster />
            <Lobby />
        </WsClientProvider>

    )
}