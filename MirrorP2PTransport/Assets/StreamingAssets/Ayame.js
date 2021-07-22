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
                // negotiated: true,
                // id: dataChannelId
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
    console.log("Ayame.js: OnConnected.");
    if (dataChannel) return;
    dataChannel = channel;
    dataChannel.onmessage = OnMessage;

    unityInstance.SendMessage(
        'AyameEventReceiver',
        'OnEvent',
        'OnConnected'
    );
}

function OnDisconnected() {
    unityInstance.SendMessage(
        'AyameEventReceiver',
        'OnEvent',
        'OnDisconnected'
    );
}

function OnMessage(e) {
    // var ptr = ArrayToReturnPtr(e.data, Uint8Array);
    let v = btoa(String.fromCharCode(...new Uint8Array(e.data)));
    unityInstance.SendMessage(
        'AyameEventReceiver',
        'OnMessage',
        v
    );
}

