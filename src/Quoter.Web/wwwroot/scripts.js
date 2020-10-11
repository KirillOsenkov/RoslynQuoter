var editor;
var resultDisplay;

function onPageLoad() {
    ace.config.set('basePath', 'https://cdnjs.cloudflare.com/ajax/libs/ace/1.4.12/');

    editor = ace.edit("inputBox");
    editor.setTheme("ace/theme/textmate");
    editor.setKeyboardHandler("ace/keyboard/vscode");
    editor.session.setMode("ace/mode/csharp");

    resultDisplay = ace.edit("outputBox",
    {
        minLines: 5,
        maxLines: 500
    });
    resultDisplay.setReadOnly(true);
    resultDisplay.setTheme("ace/theme/textmate");
    resultDisplay.setKeyboardHandler("ace/keyboard/vscode");
    resultDisplay.session.setMode("ace/mode/csharp");
}

function generateQuery() {
    var nodeKind = document.getElementById("nodeKind").value;
    var openCurlyOnNewLine = getCheckboxValue("openCurlyOnNewLine");
    var closeCurlyOnNewLine = getCheckboxValue("closeCurlyOnNewLine");
    var preserveOriginalWhitespace = getCheckboxValue("preserveOriginalWhitespace");
    var keepRedundantApiCalls = getCheckboxValue("keepRedundantApiCalls");
    var avoidUsingStatic = getCheckboxValue("avoidUsingStatic");
    var arguments = new Object();
    arguments.sourceText = editor.getValue();
    arguments.nodeKind = nodeKind;

    if (openCurlyOnNewLine) {
        arguments.openCurlyOnNewLine = true;
    }

    if (closeCurlyOnNewLine) {
        arguments.closeCurlyOnNewLine = true;
    }

    if (preserveOriginalWhitespace) {
        arguments.preserveOriginalWhitespace = true;
    }

    if (keepRedundantApiCalls) {
        arguments.keepRedundantApiCalls = true;
    }

    if (avoidUsingStatic) {
        arguments.avoidUsingStatic = true;
    }

    return arguments;
}

function onSubmitClick() {
    var arguments = generateQuery();

    getUrl(arguments, loadResults);
}

function onSubmitLINQPad() {
    var arguments = generateQuery();

    arguments.generateLINQPad = true;

    window.location = arguments;
} 

function getCheckboxValue(id) {
    return document.getElementById(id).checked;
}

function getUrl(requestArgument, callback) {
    enableSubmit(false);
    var xhr = new XMLHttpRequest();
    xhr.open("POST", "api/quoter", true);
    xhr.setRequestHeader("Accept", "application/json");
    xhr.setRequestHeader("Content-type", "application/json");
    xhr.onreadystatechange = function () {
        if (xhr.readyState == 4) {
            var data = xhr.responseText;
            if (typeof data === "string" && data.length > 0) {
                callback(data);
            }
        }

        enableSubmit(true);
    };
    xhr.send(JSON.stringify(requestArgument));
    return xhr;
}

function enableSubmit(enabled) {
    var submitButton = document.getElementById("submitButton");
    submitButton.disabled = !enabled;

    var working = document.getElementById("working");
    if (enabled) {
        working.style.display = "none";
    } else {
        working.style.display = "inline";
        setResult("");
    }
}

function setResult(data) {
    resultDisplay.setValue(data);
}

function loadResults(data) {
    // The return value is XML encoded, decode special chars
    var doc = new DOMParser().parseFromString(data, "text/html");

    setResult(doc.documentElement.textContent);
}