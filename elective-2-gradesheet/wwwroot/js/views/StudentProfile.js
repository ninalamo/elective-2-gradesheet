document.addEventListener('DOMContentLoaded', function () {
    var editModal = document.getElementById('editActivityModal');
    var tagSelect = editModal.querySelector('#editTag');
    var otherTagContainer = editModal.querySelector('#otherTagContainer');
    var otherTagInput = editModal.querySelector('#otherTag');

    // Containers for elements to hide/show
    var githubLinkContainer = editModal.querySelector('#githubLinkContainer');
    var periodContainer = editModal.querySelector('#periodContainer');
    var tagContainer = editModal.querySelector('#tagContainer');
    var maxPointsContainer = editModal.querySelector('#maxPointsContainer');
    var statusContainer = editModal.querySelector('#statusContainer');

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

        var pointsInput = editModal.querySelector('#editPoints');
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
    });
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
document.addEventListener('DOMContentLoaded', function () {
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
                        window.location.href = `@Url.Action("StudentProfile", "Home", new { id = Model.StudentId })?period=${gradingPeriod}`;
                    }, 1500); // Reload after 1.5 seconds
                }

            } catch (error) {
                console.error('Error during bulk add:', error);
                showToast('An unexpected error occurred during bulk add.', 'danger');
            }
        });
    }

    // Existing Toast Initialization and Display Logic (for TempData)
    // This part handles toasts from initial page load (e.g., after a redirect from another action)
    // This is kept for any other redirects that might use TempData.
    var toastMessageFromTempData = "@TempData["ToastMessage"]";
    var toastTypeFromTempData = "@TempData["ToastType"]";

    if (toastMessageFromTempData && toastMessageFromTempData !== "") {
        showToast(toastMessageFromTempData, toastTypeFromTempData);
    }
});