
let dropArea = document.getElementById('drop-area');
let fileArr = [];

    ;['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
        dropArea.addEventListener(eventName, preventDefaults, false)
    })

    ;['dragenter', 'dragover'].forEach(eventName => {
        dropArea.addEventListener(eventName, highlight, false)
    })

    ;['dragleave', 'drop'].forEach(eventName => {
        dropArea.addEventListener(eventName, unhighlight, false)
    })

function highlight(e) {
    dropArea.classList.add('highlight')
}

function unhighlight(e) {
    dropArea.classList.remove('highlight')
}

function preventDefaults(e) {
    e.preventDefault()
    e.stopPropagation()
}

dropArea.addEventListener('drop', handleDrop, false)

function handleDrop(e) {
    let dt = e.dataTransfer
    let files = dt.files

    handleFiles(files)
}

function handleFiles(files) {
    fileArr.length = 0;
    let uploadDiv = document.getElementById('uploadFilesDiv');
    let upload = document.getElementById('uploadFiles');
    var uploadBtn = document.getElementById('uploadFilesBtn');
    var formError = document.getElementById('formError');
    formError.style.display = "none";
    upload.innerHTML = "";
    uploadBtn.disabled = false;
    uploadDiv.style.display = "block";
    $("#folderStructure").html('');
    Object.entries(files).map((x) => {
        if (x[1].name.endsWith(".zip")) {
            let fileName = x[1].name.split(".zip")[0];
            fileArr.push(fileName);
            upload.insertAdjacentHTML('beforeend', `<div class='file-name'>${fileName}<div>`);
        } else {
            formError.style.display = "block";
            uploadBtn.disabled = true;
        }
    });
}

function uploadFiles() {
    var xmlFile = document.getElementById('xmlFile');
    var fileInfo = {
        files: fileArr,
        metadata: xmlFile.value
    }
    var url = 'YOUR URL HERE'
    var xhr = new XMLHttpRequest()
    var formData = new FormData()
    xhr.open('POST', url, true)

    var spinner = document.getElementById('spinner');
    spinner.style.display = "block";

    xhr.addEventListener('readystatechange', function (e) {
        if (xhr.readyState == 4 && xhr.status == 200) {
            // Done. Inform the user
        }
        else if (xhr.readyState == 4 && xhr.status != 200) {
            // Error. Inform the user
        }
    })

    $.ajax({
        "type": "POST",
        "url": '/Home/UploadFiles/',
        "dataType": "html",
        "contentType": "application/json",
        "data": JSON.stringify(fileInfo),
        "success": function (data) {
            $("#folderStructure").html(data);
            accordian();
            var uploadBtn = document.getElementById('uploadFilesBtn');
            var spinner = document.getElementById('spinner');
            spinner.style.display = "none";
            uploadBtn.disabled = true;
        },
        error: function (xhr, status, error) {
            console.error("Error: " + status + " " + error);
            console.error("Response Text: " + xhr.responseText);
        }
    });
}

function accordian() {
    var acc = document.getElementsByClassName("accordion");
    var i;

    for (i = 0; i < acc.length; i++) {
        acc[i].addEventListener("click", function () {
            this.classList.toggle("active");
            var panel = this.nextElementSibling;
            if (panel.style.maxHeight) {
                panel.style.maxHeight = null;
            } else {
                panel.style.maxHeight = panel.scrollHeight + "px";
            }
        });
    }
}

function loadSpinner() {
    var spinner = document.getElementById('spinner');
    spinner.style.display = "block";
}

function deleteFolders() {
    $.ajax({
        "type": "POST",
        "url": '/Home/DeleteFolders/',
        "dataType": "html",
        "contentType": "application/json",
        "data": []
    });
}