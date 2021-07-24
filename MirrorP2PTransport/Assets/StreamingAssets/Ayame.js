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
              };
            var channel = await connection.createDataChannel(dataChannelLabel,dataChannelOptions);
            if (channel) { OnConnected(channel); }
        });
        connection.on('datachannel', (channel) => { OnConnected(channel); });
        connection.on('disconnect', (e) => {
            console.log(e);
            OnDisconnected();
        });
        await connection.connect(null);
    };

    startConnection();
}

function Disconnect() {
    if (connection) {
        if (dataChannel) connection.removeDataChannel(dataChannel.label);
        connection.disconnect();
        dataChannel = null;
        connection = null;
    }
}

function SendData(data) {
    if (!IsConnectedDataChannel()) return;
    dataChannel.send(data);
}

function IsConnectedDataChannel() {
    console.log(`Ayame.js: IsConnectedDataChannel, dataChannel: ${dataChannel!==null}, readyState: ${dataChannel.readyState}`);
    if (dataChannel === null) return false;
    if (dataChannel.readyState === 'open') return true;
    if (dataChannel.readyState === `connecting`) return true;
    return false;
}

function OnConnected(channel) {
    console.log(`Ayame.js: OnConnected. label: ${channel.label}, id: ${channel.id}, readState: ${channel.readyState}`);
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
    dataChannel = null;
    connection = null;
    unityInstance.SendMessage(
        'AyameEventReceiver',
        'OnEvent',
        'OnDisconnected'
    );
}

function OnMessage(e) {
    let v = btoa(String.fromCharCode(...new Uint8Array(e.data)));
    unityInstance.SendMessage(
        'AyameEventReceiver',
        'OnMessage',
        v
    );
}

