document.addEventListener('DOMContentLoaded', function () {
    const fileInput = document.getElementById('fileUpload');
    const sectionSelect = document.getElementById('sectionSelect');
    const periodSelect = document.getElementById('periodSelect');
    const uploadButton = document.getElementById('uploadButton');

    function validateForm() {
        const file = fileInput.files[0];
        const isFileValid = file && file.name.toLowerCase().endsWith('.csv');
        const isSectionSelected = sectionSelect.value !== '';
        const isPeriodSelected = periodSelect.value !== '';

        // Enable the button only if all conditions are met
        if (isFileValid && isSectionSelected && isPeriodSelected) {
            uploadButton.disabled = false;
        } else {
            uploadButton.disabled = true;
        }
    }

    // Add event listeners to all relevant inputs
    fileInput.addEventListener('change', validateForm);
    sectionSelect.addEventListener('change', validateForm);
    periodSelect.addEventListener('change', validateForm);

    // Initial validation check on page load
    validateForm();
});
