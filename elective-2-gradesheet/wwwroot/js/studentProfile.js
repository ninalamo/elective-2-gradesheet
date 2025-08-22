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
    var maxPointsInput = editModal.querySelector('#editMaxPoints');
    var percentInput = editModal.querySelector('#editPercent');
    var scoringResultsContainer = editModal.querySelector('#scoringResultsContainer');
    var scoringResultsTable = editModal.querySelector('#scoringResultsTable tbody');
    var copyRubricButton = editModal.querySelector('#copyRubricButton');
    var prettifyJsonButton = editModal.querySelector('#prettifyJsonButton');
    var resultsTabButton = document.getElementById('results-tab');

    const transmutationTable = [
        { min: 100, max: 100, grade: 100 },
        { min: 98.40, max: 99.99, grade: 99 },
        { min: 96.80, max: 98.39, grade: 98 },
        { min: 95.20, max: 96.79, grade: 97 },
        { min: 93.60, max: 95.19, grade: 96 },
        { min: 92.00, max: 93.59, grade: 95 },
        { min: 90.40, max: 91.99, grade: 94 },
        { min: 88.80, max: 90.39, grade: 93 },
        { min: 87.20, max: 88.79, grade: 92 },
        { min: 85.60, max: 87.19, grade: 91 },
        { min: 84.00, max: 85.59, grade: 90 },
        { min: 82.40, max: 83.99, grade: 89 },
        { min: 80.80, max: 82.39, grade: 88 },
        { min: 79.20, max: 80.79, grade: 87 },
        { min: 77.60, max: 79.19, grade: 86 },
        { min: 76.00, max: 77.59, grade: 85 },
        { min: 74.40, max: 75.99, grade: 84 },
        { min: 72.80, max: 74.39, grade: 83 },
        { min: 71.20, max: 72.79, grade: 82 },
        { min: 69.60, max: 71.19, grade: 81 },
        { min: 68.00, max: 69.59, grade: 80 },
        { min: 66.40, max: 67.99, grade: 79 },
        { min: 64.80, max: 66.39, grade: 78 },
        { min: 63.20, max: 64.79, grade: 77 },
        { min: 61.60, max: 63.19, grade: 76 },
        { min: 60.00, max: 61.59, grade: 75 },
        { min: 56.00, max: 59.99, grade: 74 },
        { min: 52.00, max: 55.99, grade: 73 },
        { min: 48.00, max: 51.99, grade: 72 },
        { min: 44.00, max: 47.99, grade: 71 },
        { min: 40.00, max: 43.99, grade: 70 },
        { min: 36.00, max: 39.99, grade: 69 },
        { min: 32.00, max: 35.99, grade: 68 },
        { min: 28.00, max: 31.99, grade: 67 },
        { min: 24.00, max: 27.99, grade: 66 },
        { min: 20.00, max: 23.99, grade: 65 },
        { min: 16.00, max: 19.99, grade: 64 },
        { min: 12.00, max: 15.99, grade: 63 },
        { min: 8.00, max: 11.99, grade: 62 },
        { min: 4.00, max: 7.99, grade: 61 },
        { min: 0, max: 3.99, grade: 60 }
    ];

    function calculateTransmutedGrade() {
        const score = parseFloat(pointsInput.value);
        const max = parseFloat(maxPointsInput.value);

        if (isNaN(score) || isNaN(max) || max === 0) {
            percentInput.value = '';
            return;
        }

        const percentage = (score / max) * 100;
        let finalGrade = 60; // Default to the lowest grade

        for (const range of transmutationTable) {
            if (percentage >= range.min && percentage <= range.max) {
                finalGrade = range.grade;
                break;
            }
        }
        percentInput.value = finalGrade;
    }

    pointsInput.addEventListener('input', calculateTransmutedGrade);
    maxPointsInput.addEventListener('input', calculateTransmutedGrade);

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

        try {
            const rubric = JSON.parse(rubricJson.value);
            const totalPoints = rubric.reduce((sum, item) => sum + Number(item.points), 0);

            if (totalPoints !== 100) {
                showToast(`Rubric points must total 100. Current total: ${totalPoints}`, 'danger');
                return;
            }
        } catch (e) {
            showToast('Invalid JSON format. Please correct it before checking.', 'danger');
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
                calculateTransmutedGrade(); // Calculate percent after scoring
                scoringResultsTable.innerHTML = '';
                data.results.forEach(result => {
                    let row = scoringResultsTable.insertRow();
                    row.className = result.met ? 'table-success' : 'table-danger';
                    let metCell = row.insertCell();
                    metCell.innerHTML = result.met ? '<i class="fas fa-check-circle text-success"></i>' : '<i class="fas fa-times-circle text-danger"></i>';
                    row.insertCell().textContent = result.fileName;
                    row.insertCell().textContent = result.criterion;
                    row.insertCell().textContent = result.points;
                    let proofCell = row.insertCell();
                    const proofText = result.proof || "N/A";
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

        // Reset tabs
        var detailsTab = new bootstrap.Tab(document.getElementById('details-tab'));
        detailsTab.show();
        resultsTabButton.style.display = 'none';


        activityIdInput.value = activityId;
        activityNameP.textContent = activityName;
        activityNameHiddenInput.value = activityName;
        pointsInput.value = points;
        maxPointsInput.value = maxPoints;
        calculateTransmutedGrade(); // Calculate on open
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