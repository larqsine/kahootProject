import {useWsClient} from "ws-request-hook";
import {useEffect, useState} from "react";

export default function Lobby() {
    
    const {onMessage, sendRequest, send, readyState} = useWsClient();
   

    useEffect(() => {
        if (readyState !== 1) return;
        //communicate with server here
    }, [readyState]); 

    return (<>
        <>hello world</>

    </>)
}