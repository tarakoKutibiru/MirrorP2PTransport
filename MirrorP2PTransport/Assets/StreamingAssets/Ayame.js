let dataChannel = null;
let connection = null;

function Connect(signalingUrl, roomId, signalingKey) {
    const options = Ayame.defaultOptions;
    options.signalingKey = signalingKey;

    const startConnection = async () => {
        connection = Ayame.connection(signalingUrl, roomId, options, true);
        connection.on('open', async (e) => {
            var channel = await connection.createDataChannel('datachannel');
            if (channel) { OnConnected(channel); }
        });
        connection.on('datachannel', (channel) => { OnConnected(channel); });
        connection.on('disconnect', (e) => {
            console.log(e);
            OnDisconnected();
            dataChannel = null;
        });
        await connection.connect(null);
    };

    startConnection();
}

function Disconnect() {
    if (connection) {
        connection.disconnect();
    }
}

function SendData(data) {
    if (!IsConnectedDataChannel()) return;
    dataChannel.send(data);
}

function IsConnectedDataChannel() {
    return dataChannel && dataChannel.readyState === 'open';
}

function OnConnected(channel) {
    if (dataChannel) return;
    dataChannel = channel;
    dataChannel.onmessage = OnMessage;

    unityInstance.SendMessage(
        'Ayame',
        'OnEvent',
        'OnConnected'
    );
}

function OnDisconnected() {
    unityInstance.SendMessage(
        'Ayame',
        'OnEvent',
        'OnDisconnected'
    );
}

function OnMessage(e) {
    unityInstance.SendMessage(
        'Ayame',
        'OnMessage',
        e.data
    );
}