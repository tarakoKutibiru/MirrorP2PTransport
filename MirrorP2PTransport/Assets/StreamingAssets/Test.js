let dataChannel = null;

function ShowTestJsHelloWorld() {
    window.alert("Test.js: HelloWorld!!");
}

function StartSignaling(signalingUrl, roomId, signalingKey) {
    const options = Ayame.defaultOptions;
    options.signalingKey = signalingKey;

    const startConnection = async () => {
        const connection = Ayame.connection(signalingUrl, roomId, options, true);
        connection.on('open', async (e) => {
            dataChannel = await connection.createDataChannel('datachannel');
            if (dataChannel) {
                dataChannel.onmessage = onMessage;
            }
        });
        connection.on('datachannel', (channel) => {
            if (!dataChannel) {
                dataChannel = channel;
                dataChannel.onmessage = onMessage;
            }
        });
        connection.on('disconnect', (e) => {
            console.log(e);
            dataChannel = null;
        });
        await connection.connect(null);
    };

    startConnection();
};

function SendData(data) {
    if (!IsConnectedDataChannel()) return;
    dataChannel.send(data);
}

function IsConnectedDataChannel() {
    return dataChannel && dataChannel.readyState === 'open';
}

function onMessage(e) {
    console.log('data received: ', e.data);
}

function AsyncAwaitTest() {
    const _sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

    const hoge = async () => {
        console.log("b");
        await _sleep(2000);
        console.log("c");
        return "d";
    }

    console.log("a");
    const h = hoge();
    console.log(h);
    console.log("e");
}