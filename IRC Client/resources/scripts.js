var csharp = window.external;

function send() {
    // Enter key pressed
    if (event.keyCode == 13) {
        var tmp = document.getElementById('textbox').value;
        document.getElementById('textbox').value = '';
        csharp.Send(tmp);
    }
}

function scroll(window) {
    var objDiv = document.getElementById(window);
    if (objDiv) {
        objDiv.scrollTop = objDiv.scrollHeight;
    }
}