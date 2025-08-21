document.addEventListener('DOMContentLoaded', function () {
    var editModal = document.getElementById('editActivityModal');
    var tagSelect = editModal.querySelector('#editTag');
    var otherTagContainer = editModal.querySelector('#otherTagContainer');
    var otherTagInput = editModal.querySelector('#otherTag');
    var githubLinkContainer = editModal.querySelector('#githubLinkContainer');
    var periodContainer = editModal.querySelector('#periodContainer');
    var tagContainer = editModal.querySelector('#tagContainer');
    var maxPointsContainer = editModal.querySelector('#maxPointsContainer');
    var statusContainer = editModal.querySelector('#statusContainer');
    var checkButton = editModal.querySelector('#checkButton');
    var fileUpload = editModal.querySelector('#fileUpload');
    var fileTreeView = editModal.querySelector('#fileTreeView');
    var rubricJson = editModal.querySelector('#rubricJson');
    var pointsInput = editModal.querySelector('#editPoints');
    var scoringResultsContainer = editModal.querySelector('#scoringResultsContainer');
    var scoringResultsTable = editModal.querySelector('#scoringResultsTable tbody');
    var copyRubricButton = editModal.querySelector('#copyRubricButton');
    var prettifyJsonButton = editModal.querySelector('#prettifyJsonButton');
    var resultsTabButton = document.getElementById('results-tab');

    function toggleCheckButton() {
        checkButton.disabled = fileUpload.files.length === 0 || rubricJson.value.trim() === '';
    }

    tagSelect.addEventListener('change', function () {
        if (this.value === 'Other') {
            otherTagContainer.style.display = 'block';
            otherTagInput.required = true;
        } else {
            otherTagContainer.style.display = 'none';
            otherTagInput.required = false;
            otherTagInput.value = '';
        }
    });

    fileUpload.addEventListener('change', function (event) {
        const files = event.target.files;
        fileTreeView.innerHTML = '';
        if (files.length > 0) {
            const tree = buildTree(files);
            renderTree(tree, fileTreeView);
        }
        toggleCheckButton();
    });

    rubricJson.addEventListener('input', toggleCheckButton);

    copyRubricButton.addEventListener('click', function () {
        rubricJson.select();
        document.execCommand('copy');
        showToast('Rubric copied to clipboard', 'info');
    });

    prettifyJsonButton.addEventListener('click', function () {
        try {
            const ugly = rubricJson.value;
            const obj = JSON.parse(ugly);
            const pretty = JSON.stringify(obj, undefined, 4);
            rubricJson.value = pretty;
            showToast('JSON formatted successfully!', 'success');
        } catch (e) {
            showToast('Invalid JSON. Please correct and try again.', 'danger');
        }
    });

    checkButton.addEventListener('click', async function () {
        if (pointsInput.value !== "0") {
            showToast("Scoring is only available for activities with zero points.", "info");
            return;
        }

        const formData = new FormData();
        const files = fileUpload.files;
        for (let i = 0; i < files.length; i++) {
            formData.append('files', files[i]);
            formData.append('filePaths', files[i].webkitRelativePath || files[i].name);
        }
        formData.append('rubricJson', rubricJson.value);

        try {
            const response = await fetch('/Home/ScoreActivity', {
                method: 'POST',
                body: formData
            });

            if (response.ok) {
                const data = await response.json();
                pointsInput.value = data.totalScore;
                scoringResultsTable.innerHTML = '';
                data.results.forEach(result => {
                    let row = scoringResultsTable.insertRow();
                    row.insertCell().textContent = result.fileName;
                    row.insertCell().textContent = result.criterion;
                    row.insertCell().textContent = result.points;
                    let proofCell = row.insertCell();
                    const proofText = result.proof || "Proof not available";
                    proofCell.innerHTML = `<pre><code>${proofText}</code></pre>`;
                });
                resultsTabButton.style.display = 'block'; // Show the results tab
                var resultsTab = new bootstrap.Tab(resultsTabButton);
                resultsTab.show(); // Switch to the results tab
                showToast(`Scoring complete. Total points: ${data.totalScore}`, "success");

            } else {
                const error = await response.json();
                showToast(error.message, "danger");
            }
        } catch (error) {
            showToast("An error occurred during scoring.", "danger");
        }
    });

    function buildTree(files) {
        const tree = {};
        for (const file of files) {
            const path = (file.webkitRelativePath || file.name).split('/');
            let currentLevel = tree;
            for (let i = 0; i < path.length; i++) {
                const part = path[i];
                if (!currentLevel[part]) {
                    currentLevel[part] = i === path.length - 1 ? file : {};
                }
                currentLevel = currentLevel[part];
            }
        }
        return tree;
    }

    function renderTree(tree, container, currentPath = '') {
        const ul = document.createElement('ul');
        ul.className = 'tree-container';

        for (const key in tree) {
            const li = document.createElement('li');
            li.className = 'tree-node';

            const nodeName = document.createElement('span');
            nodeName.className = 'node-name';
            nodeName.textContent = key;

            const isFile = tree[key] instanceof File;
            const newPath = currentPath ? `${currentPath}/${key}` : key;

            const removeBtn = document.createElement('span');
            removeBtn.className = 'remove-btn';
            removeBtn.textContent = 'x';
            removeBtn.onclick = function (e) {
                e.stopPropagation();
                const files = Array.from(fileUpload.files);
                let newFiles;

                if (isFile) {
                    newFiles = files.filter(f => (f.webkitRelativePath || f.name) !== newPath);
                } else {
                    newFiles = files.filter(f => !(f.webkitRelativePath || f.name).startsWith(newPath + '/'));
                }

                const dataTransfer = new DataTransfer();
                newFiles.forEach(file => dataTransfer.items.add(file));
                fileUpload.files = dataTransfer.files;
                li.remove();
                toggleCheckButton();
            };

            if (isFile) {
                li.classList.add('node-file');
                li.appendChild(nodeName);
                li.appendChild(removeBtn);
            } else {
                li.classList.add('expanded');
                const childrenContainer = document.createElement('div');
                childrenContainer.className = 'children';
                renderTree(tree[key], childrenContainer, newPath);
                nodeName.onclick = function () {
                    li.classList.toggle('expanded');
                };
                li.appendChild(nodeName);
                li.appendChild(removeBtn);
                li.appendChild(childrenContainer);
            }
            ul.appendChild(li);
        }
        container.appendChild(ul);
    }

    editModal.addEventListener('show.bs.modal', function (event) {
        var button = event.relatedTarget;
        var activityId = button.getAttribute('data-activity-id');
        var newId = button.getAttribute('data-new-id');
        var activityName = button.getAttribute('data-activity-name');
        var points = button.getAttribute('data-points');
        var maxPoints = button.getAttribute('data-max-points');
        var period = button.getAttribute('data-period');
        var tag = button.getAttribute('data-tag');
        var githubLink = button.getAttribute('data-github-link');
        var status = button.getAttribute('data-status');

        var activityIdInput = editModal.querySelector('#editActivityId');
        var activityNameP = editModal.querySelector('#editActivityName');
        var activityNameHiddenInput = editModal.querySelector('#editActivityNameInput');

        var periodSelect = editModal.querySelector('#editPeriod');
        var tagSelect = editModal.querySelector('#editTag');
        var githubLinkInput = editModal.querySelector('#editGithubLink');
        var maxPointsInput = editModal.querySelector('#editMaxPoints');
        var statusSelect = editModal.querySelector('#editStatus');

        activityIdInput.value = activityId;
        activityNameP.textContent = activityName;
        activityNameHiddenInput.value = activityName;
        pointsInput.value = points;
        // Set period dropdown
        for (var i = 0; i < periodSelect.options.length; i++) {
            if (periodSelect.options[i].text.toUpperCase() === period.toUpperCase()) {
                periodSelect.selectedIndex = i;
                break;
            }
        }

        // Set status dropdown
        statusSelect.value = status;

        if (points !== "0") {
            document.getElementById('rubric-tab').style.display = 'none';
            resultsTabButton.style.display = 'none';
        } else {
            document.getElementById('rubric-tab').style.display = 'block';
        }

        // Determine form behavior based on status
        if (status === "Missing") {
            githubLinkContainer.style.display = 'block';
            periodContainer.style.display = 'block';
            tagContainer.style.display = 'block';
            maxPointsContainer.style.display = 'block';
            statusContainer.style.display = 'block';

            githubLinkInput.value = '';
            pointsInput.value = '';
            maxPointsInput.value = maxPoints;

            statusSelect.value = "Added";

            var tagOptions = ['Assignment', 'Hands-on'];
            if (tag && tagOptions.includes(tag)) {
                tagSelect.value = tag;
                otherTagContainer.style.display = 'none';
                otherTagInput.required = false;
            } else {
                tagSelect.value = 'Other';
                otherTagContainer.style.display = 'block';
                otherTagInput.required = true;
                otherTagInput.value = tag || '';
            }

        } else {
            githubLinkContainer.style.display = 'block';
            periodContainer.style.display = 'none';
            tagContainer.style.display = 'none';
            maxPointsContainer.style.display = 'block';
            statusContainer.style.display = 'none';

            maxPointsInput.readOnly = true;

            githubLinkInput.value = githubLink;
            maxPointsInput.value = maxPoints;

            githubLinkInput.required = false;
            tagSelect.required = false;
            periodSelect.required = false;
            maxPointsInput.required = false;
            statusSelect.required = false;
            var tagOptions = ['Assignment', 'Hands-on'];
            if (tag && tagOptions.includes(tag)) {
                tagSelect.value = tag;
                otherTagContainer.style.display = 'none';
                otherTagInput.required = false;
            } else {
                tagSelect.value = 'Other';
                otherTagContainer.style.display = 'none';
                otherTagInput.required = true;
                otherTagInput.value = tag || '';
            }
        }
        toggleCheckButton();
    });

    // Reusable Toast Display Function
    function showToast(message, type) {
        var toastElement = document.getElementById('liveToast');
        var toastHeader = document.getElementById('toastHeader');
        var toastBody = document.getElementById('toastBody');

        // Clear previous classes
        toastElement.classList.remove('text-bg-success', 'text-bg-info', 'text-bg-danger');
        toastBody.textContent = message;
        toastElement.classList.add('text-bg-' + type);
        // Set header text (e.g., "Success", "Info", "Danger")
        toastHeader.textContent = type.charAt(0).toUpperCase() + type.slice(1);
        var toast = new bootstrap.Toast(toastElement);
        toast.show();
    }

    // Bulk Add Form Submission Handler
    var bulkAddForm = document.getElementById('bulkAddForm');

    if (bulkAddForm) {
        bulkAddForm.addEventListener('submit', async function (event) {
            event.preventDefault(); // Prevent default form submission (page refresh)

            const studentId = this.querySelector('input[name="studentId"]').value;
            const gradingPeriodSelect = this.querySelector('select[name="gradingPeriod"]');
            const gradingPeriod = gradingPeriodSelect.value;

            if (!gradingPeriod) {
                showToast("Please select a grading period for bulk add.", "danger");
                return;
            }

            const formData = new FormData();
            formData.append('studentId', studentId);
            formData.append('gradingPeriod', gradingPeriod);
            try {
                const response = await fetch(this.action, {
                    method: 'POST',
                    body: formData,
                    headers: {
                        // Include anti-forgery token from the hidden input
                        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                    }
                });
                const result = await response.json();

                showToast(result.message, result.type);

                // If activities were added, refresh the page after a short delay
                if (result.success && result.addedCount > 0) {
                    setTimeout(() => {
                        // Reload the page with the current student ID and selected period to show updated data
                        window.location.href = window.location.pathname + `?period=${gradingPeriod}`;
                    }, 1500); // Reload after 1.5 seconds
                }

            } catch (error) {
                console.error('Error during bulk add:', error);
                showToast('An unexpected error occurred during bulk add.', 'danger');
            }
        });
    }

    // Existing Toast Initialization and Display Logic (for TempData)
    var toastMessageFromTempData = document.body.getAttribute('data-toast-message');
    var toastTypeFromTempData = document.body.getAttribute('data-toast-type');

    if (toastMessageFromTempData && toastMessageFromTempData !== "") {
        showToast(toastMessageFromTempData, toastTypeFromTempData);
    }
});