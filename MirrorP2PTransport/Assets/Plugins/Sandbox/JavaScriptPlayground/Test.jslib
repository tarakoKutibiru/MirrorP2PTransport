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

    ShowTestJsHelloWorld: function()
    {
        ShowTestJsHelloWorld();
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
});