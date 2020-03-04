var cs = window.external;

function send() {
    // Enter key pressed
    if (event.keyCode == 13) {
        var tmp = document.getElementById('textbox').value;
        document.getElementById('textbox').value = '';
        cs.Send(tmp);
    }
}
function scroll() {
    //window.scrollTo(0, document.body.scrollHeight);
    var objDiv = document.getElementById("main");
    objDiv.scrollTop = objDiv.scrollHeight;
}