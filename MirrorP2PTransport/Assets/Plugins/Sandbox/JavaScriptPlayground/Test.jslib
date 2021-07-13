mergeInto(LibraryManager.library,
{
    HelloWorld: function()
    {
        window.alert("Hello World!");
    },

    HelloWorldString: function(str)
    {
        window.alert(Pointer_stringify(str));
    },
});