let dataChannel = null;
let connection = null;

function Hoge()
{
    console.log("hoge");
}

function Connect(signalingUrl, roomId, signalingKey,dataChannelLabel,dataChannelId) {
    console.log("js: Connect");
    const options = Ayame.defaultOptions;
    options.signalingKey = signalingKey;

    const startConnection = async () => {
        connection = Ayame.connection(signalingUrl, roomId, options, true);
        connection.on('open', async (e) => {
            var dataChannelOptions = {
                ordered: true,
                negotiated: true,
                id: dataChannelId
              };
            var channel = await connection.createDataChannel(dataChannelLabel,dataChannelOptions);
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
    var ptr = utils.arrayToReturnPtr(e.data, Uint8Array);
    unityInstance.SendMessage(
        'Ayame',
        'OnMessage',
        ptr
    );
}