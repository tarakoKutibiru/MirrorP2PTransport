mergeInto(LibraryManager.library,
{
    Connect: function(signalingUrl, signalingKey, roomId, dataChannelLabel, dataChannelId)
    {
        console.log("jslib: Connect");
        Connect(Pointer_stringify(signalingUrl), Pointer_stringify(roomId), Pointer_stringify(signalingKey),Pointer_stringify(dataChannelLabel), dataChannelId);
    },

    Disconnect: function()
    {
        Disconnect();
    },

    SendData: function(data, size)
    {
        var byteArray = HEAPU8.subarray(data, data + size);
        SendData(byteArray);
    },

    IsConnected: function()
    {
        return IsConnectedDataChannel();
    },
    InjectionJs:function(url,id)
    {
        url=Pointer_stringify(url);
        id=Pointer_stringify(id);
        if(!document.getElementById(id))
        {
            var s = document.createElement("script");
            s.setAttribute('src',url);
            s.setAttribute('id',id);
            document.head.appendChild(s);
        }
    },

    ExecFree:function(ptr) {
        _free(ptr);
    },
});