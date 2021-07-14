mergeInto(LibraryManager.library,
{
    Connect: function(signalingUrl, signalingKey, roomId)
    {
        Connect(Pointer_stringify(signalingUrl), Pointer_stringify(roomId), Pointer_stringify(signalingKey));
    },

    Disconnect: function()
    {
        Disconnect();
    },

    SendData: function(message)
    {
        SendData(Pointer_stringify(message));
    },
});