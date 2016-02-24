function onPageLoad() {
}

function onSubmitClick() {
    var openCurlyOnNewLine = getCheckboxValue("openCurlyOnNewLine");
    var closeCurlyOnNewLine = getCheckboxValue("closeCurlyOnNewLine");
    var preserveOriginalWhitespace = getCheckboxValue("preserveOriginalWhitespace");
    var keepRedundantApiCalls = getCheckboxValue("keepRedundantApiCalls");
    var avoidUsingStatic = getCheckboxValue("avoidUsingStatic");
    var query = "api/quoter/?sourceText=" + encodeURIComponent(inputBox.value);

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

    getUrl(query, loadResults);
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
    var container = document.getElementById("outputDiv");
    if (container) {
        container.innerHTML = data;
    }
}

function loadResults(data) {
    setResult(data);
}