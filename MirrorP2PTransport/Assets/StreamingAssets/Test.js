
function ShowTestJsHelloWorld() {
    window.alert("Test.js: HelloWorld!!");
}

function StartSignaling(signalingUrl, roomId, signalingKey) {
    let dataChannel = null;

    const options = Ayame.defaultOptions;
    options.signalingKey = signalingKey;

    const startConn = async () => {
        const conn = Ayame.connection(signalingUrl, roomId, options, true);
        conn.on('open', async (e) => {
            dataChannel = await conn.createDataChannel('datachannel');
            if (dataChannel == null) {
                console.log("dataChannel is nul");
            }

            dataChannel.onmessage = (e) => {
                console.log('data received: ', e.data);
            };
        });
        await conn.connect(null);
    };
    startConn();

    const sendData = (data) => {
        dataChannel.send(data);
    };
}