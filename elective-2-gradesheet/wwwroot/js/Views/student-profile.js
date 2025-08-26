// wwwroot/js/views/student-profile.js
document.addEventListener('DOMContentLoaded', function () {
    // Enable Bootstrap tooltips
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.forEach(function (tooltipTriggerEl) {
        new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Example: modal open events (if needed later)
    const editButtons = document.querySelectorAll('[data-bs-target="#editActivityModal"]');
    editButtons.forEach(function (btn) {
        btn.addEventListener('click', function () {
            console.log("Opening edit modal for activity...");
            // You can add dynamic modal population code here
        });
    });
});
