
function fileSaveAs(fileName, dataPtr, size) {

    var buffer = new Uint8Array(size); // data length

    for (var i = 0; i < size; i++) {
        buffer[i] = Module.getValue(dataPtr + i, "i8");
    }

    var a = window.document.createElement('a');
    var blob = new Blob([buffer]);
    a.href = window.URL.createObjectURL(blob);
    a.download = fileName;

    // Append anchor to body.
    document.body.appendChild(a);
    a.click();

    // Remove anchor from body
    document.body.removeChild(a); 
}