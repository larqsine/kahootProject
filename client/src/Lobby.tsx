import {useWsClient} from "ws-request-hook";
import {useEffect, useState} from "react";

export default function Lobby() {
    
    const {onMessage, sendRequest, send, readyState} = useWsClient();
    const [clients, setClients] = useState<string[]>([''])

    useEffect(() => {
        if (readyState !== 1) return;
        //communicate with server here
    }, [readyState]); 

    return (<>
        <div>Clients in the lobbby:</div>
        {
            clients.map(c => <div key={c}>{c}</div>)
        }

    </>)
}