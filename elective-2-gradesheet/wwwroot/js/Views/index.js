// wwwroot/js/views/index.js
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

        uploadButton.disabled = !(isFileValid && isSectionSelected && isPeriodSelected);
    }

    fileInput.addEventListener('change', validateForm);
    sectionSelect.addEventListener('change', validateForm);
    periodSelect.addEventListener('change', validateForm);

    // Initial check
    validateForm();
});
