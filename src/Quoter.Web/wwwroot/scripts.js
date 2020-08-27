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
    var query = "api/quoter/?sourceText=" + encodeURIComponent(editor.getValue());

    query = query + "&nodeKind=" + nodeKind;

    if (openCurlyOnNewLine) {
        query = query + "&openCurlyOnNewLine=true";
    }

    if (closeCurlyOnNewLine) {
        query = query + "&closeCurlyOnNewLine=true";
    }

    if (preserveOriginalWhitespace) {
        query = query + "&preserveOriginalWhitespace=true";
    }

    if (keepRedundantApiCalls) {
        query = query + "&keepRedundantApiCalls=true";
    }

    if (avoidUsingStatic) {
        query = query + "&avoidUsingStatic=true";
    }

    return query;
}

function onSubmitClick() {
    var query = generateQuery();

    getUrl(query, loadResults);
}

function onSubmitLINQPad() {
    var query = generateQuery();

    query = query + "&generateLINQPad=true";

    window.location = query;
}

function getCheckboxValue(id) {
    return document.getElementById(id).checked;
}

function getUrl(url, callback) {
    enableSubmit(false);
    var xhr = new XMLHttpRequest();
    xhr.open("GET", url, true);
    xhr.setRequestHeader("Accept", "text/html");
    xhr.onreadystatechange = function () {
        if (xhr.readyState == 4) {
            var data = xhr.responseText;
            if (typeof data === "string" && data.length > 0) {
                callback(data);
            }
        }

        enableSubmit(true);
    };
    xhr.send();
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