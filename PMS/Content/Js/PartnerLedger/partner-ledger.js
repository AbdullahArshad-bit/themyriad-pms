var isPasswordValidated = false; // Security flag to prevent direct access to payment modal

// Function to disable autocomplete and clear email from search input
function disableSearchAutocomplete() {
    var searchInput = $('#Ledger_filter input[type="search"]');
    if (searchInput.length) {
        // Set all autocomplete-related attributes to prevent browser autofill
        searchInput.attr('autocomplete', 'off');
        searchInput.attr('autocapitalize', 'off');
        searchInput.attr('autocorrect', 'off');
        searchInput.attr('spellcheck', 'false');
        searchInput.attr('data-lpignore', 'true'); // LastPass ignore
        searchInput.attr('data-form-type', 'other'); // Prevent password managers
        searchInput.attr('name', 'datatables-search'); // Change name to prevent email autofill
        searchInput.attr('id', 'ledger-search-input'); // Set specific ID
        
        // Function to check if value looks like an email
        function isEmailLike(val) {
            if (!val) return false;
            // More robust email detection - check for @ and domain pattern
            var emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            return emailPattern.test(val.trim());
        }
        
        // Clear email value if present
        var currentVal = searchInput.val();
        if (isEmailLike(currentVal)) {
            searchInput.val('');
        }
        
        // Add event listeners to clear email on various events (using namespace to avoid conflicts)
        searchInput.off('focus.searchfix click.searchfix input.searchfix change.searchfix').on('focus.searchfix click.searchfix input.searchfix change.searchfix', function() {
            var val = $(this).val();
            if (isEmailLike(val)) {
                $(this).val('');
            }
        });
        
        // Also clear on paste
        searchInput.off('paste.searchfix').on('paste.searchfix', function(e) {
            setTimeout(function() {
                var val = searchInput.val();
                if (isEmailLike(val)) {
                    searchInput.val('');
                }
            }, 10);
        });
        
        // Clear on blur as well (when user clicks away)
        searchInput.off('blur.searchfix').on('blur.searchfix', function() {
            var val = $(this).val();
            if (isEmailLike(val)) {
                $(this).val('');
            }
        });
        
        return true; // Input found and processed
    }
    return false; // Input not found yet
}

$(document).ready(function () {
    // Use MutationObserver to catch the search input as soon as it's created by DataTables
    var observer = new MutationObserver(function(mutations) {
        mutations.forEach(function(mutation) {
            if (mutation.addedNodes.length) {
                disableSearchAutocomplete();
            }
        });
    });
    
    // Start observing the document body for new nodes
    observer.observe(document.body, {
        childList: true,
        subtree: true
    });
    
    $('#Ledger').DataTable({
        "scrollY": "70vh",
        "scrollCollapse": true,
        "scrollX": "auto",
        "pageLength": -1,
        buttons: [
            'excelHtml5',
        ],
        dom: 'Blfrtip',
        lengthMenu: [[10, 25, 50, 100, -1], [10, 25, 50, 100, "All"]],
        columnDefs: [
            { type: 'date-euro', targets: 3 }
        ],
        "order": [[3, 'asc']],
        "initComplete": function (settings, json) {
            applyRefundStyling();
            disableSearchAutocomplete();
        }
    });
    $('.dataTables_length').addClass('bs-select');
    
    // Multiple safeguards with different delays to catch the input at various stages
    setTimeout(disableSearchAutocomplete, 50);
    setTimeout(disableSearchAutocomplete, 100);
    setTimeout(disableSearchAutocomplete, 200);
    setTimeout(disableSearchAutocomplete, 500);
    setTimeout(disableSearchAutocomplete, 1000);
    
    // Continuous check every 300ms for the first 5 seconds (in case of slow rendering)
    var checkInterval = setInterval(function() {
        if (disableSearchAutocomplete()) {
            // If input found and processed, we can stop checking after a few more attempts
            setTimeout(function() {
                clearInterval(checkInterval);
            }, 1000);
        }
    }, 300);
    
    // Stop the interval after 5 seconds regardless
    setTimeout(function() {
        clearInterval(checkInterval);
        observer.disconnect(); // Clean up observer
    }, 5000);
    
    // Additional safeguard on window load (in case page loads slowly)
    $(window).on('load', function() {
        setTimeout(disableSearchAutocomplete, 100);
        setTimeout(disableSearchAutocomplete, 500);
    });

    $("#DebitAmount").on("keyup change", onDebitAmountChange);
    $('#paymentModalForm').validate({
        rules: {
            DebitAmount: { required: true, min: 0.01, max: PartnerLedgerConfig.totalBalanceAbs, number: true },
            PaymentTypeId: { required: true },
            Remarks: { required: true, minlength: 3 }
        },
        messages: {
            DebitAmount: { required: "Please enter the debit amount", min: "Amount must be greater than 0", max: "Amount cannot exceed the outstanding balance", number: "Please enter a valid number" },
            PaymentTypeId: { required: "Please select a payment method" },
            Remarks: { required: "Please enter remarks", minlength: "Remarks must be at least 3 characters long" }
        },
        errorElement: 'span',
        errorClass: 'field-validation-error',
        highlight: function (element) { $(element).addClass('input-validation-error'); },
        unhighlight: function (element) { $(element).removeClass('input-validation-error'); }
    });

    $("#PaymentModal").on("hide.bs.modal", function () {
        $("#DebitAmount").val("");
        $('#paymentConfirmation').hide();
        isPasswordValidated = false; // Reset security flag when modal closes
    });

    initDatePickers();
    
    // Eye toggle for transaction password
    $(".toggle-transaction-password").click(function () {
        $(this).toggleClass("fa-eye fa-eye-slash");
        var input = $($(this).data('element'));
        if (input.attr("type") == "password") {
            input.attr("type", "text");
        } else {
            input.attr("type", "password");
        }
    });
});

function applyRefundStyling() {
    $('#Ledger tbody tr').each(function () {
        var $row = $(this);
        var reference = $row.find('td:eq(1)').text();
        var type = $row.find('td:eq(2)').text();

        if (reference && (reference.toLowerCase().includes('ref-inv') || reference.toLowerCase().includes('r-inv') || reference.toLowerCase().includes('reversal'))) {
            var $creditCell = $row.find('td:eq(7)');
            if ($creditCell.text() && $creditCell.text() !== '-') { $creditCell.addClass('amount-refund-invoice'); }
        } else if (type && type.toLowerCase().includes('refund') && reference && reference.toLowerCase().includes('ref')) {
            var $creditCell = $row.find('td:eq(7)');
            if ($creditCell.text() && $creditCell.text() !== '-') { $creditCell.addClass('amount-refund-payment'); }
        }
    });
}

function GetLedger() {
    var Status = $('#Status').val();
    var PersonId = PartnerLedgerConfig.personId;
    var SD = $('.from_date').val();
    var ED = $('.to_date').val();
    window.location.href = GetFormatedUrl("/Payments/PartnerLedger?PersonId=" + PersonId + "&Status=" + Status + "&FromDate=" + SD + "&ToDate=" + ED);
};

function OpenPaymentModal() {
    // Security check: Only allow if password was validated
    //if (!isPasswordValidated) {
    //    alert('Access denied. Please use the "Pay Refund" button to proceed.');
    //    return;
    //}
    
    $("#PaymentModal").modal();
    $('#paymentModalForm').find("input[type=text], textarea").val("");
    $('#paymentConfirmation').hide();
    $('.btn-ok').prop('disabled', false);
}

function PostPaymentModal() {
    // Validate the form
    if (!$('#paymentModalForm').valid()) {
        return; // Exit if validation fails
    }

    // Disable the "Yes" button to prevent multiple clicks
    $('.btn-ok').prop('disabled', true);

    // Show the loader while the form is being processed
    $('.Customoverlay, .Customloader').show();

    // Submit the form
    $('#paymentModalForm').submit();
    // Hide modal after submit is triggered to avoid interfering with form submission lifecycle
    setTimeout(function(){ $('#PaymentModal').modal('hide'); }, 50);
}

function onDebitAmountChange() {
    var outstandingBalance = parseFloat(PartnerLedgerConfig.totalBalanceAbs);
    var debitAmount = parseFloat($("#DebitAmount").val());

    if (isNaN(debitAmount) || debitAmount <= 0) { $('#paymentConfirmation').hide(); return; }
    if (debitAmount > outstandingBalance) { $("#DebitAmount").val(""); $('#paymentConfirmation').hide(); return; }
    if (debitAmount < outstandingBalance) { $('#paymentConfirmation').show(); }
    else if (debitAmount === outstandingBalance) { $('#paymentConfirmation').hide(); }
}

function initDatePickers() {
    var startDate = new Date();
    var FromEndDate = new Date();
    var ToEndDate = new Date();
    ToEndDate.setDate(ToEndDate.getDate() + 365);

    $('.from_date').datepicker({
        weekStart: 1,
        startDate: '',
        format: "dd/M/yyyy",
        orientation: "bottom left",
        autoclose: true
    }).on('changeDate', function (selected) {
        startDate = new Date(selected.date.valueOf());
        $('.to_date').datepicker('setStartDate', startDate);
    });

    $('.to_date').datepicker({
        weekStart: 1,
        startDate: new Date($('.from_date').val()),
        endDate: ToEndDate,
        format: "dd/M/yyyy",
        orientation: "bottom left",
        autoclose: true,
    }).on('changeDate', function (selected) {
        FromEndDate = new Date(selected.date.valueOf());
        FromEndDate.setDate(FromEndDate.getDate(new Date(selected.date.valueOf())));
        $('.from_date').datepicker('setEndDate', FromEndDate);
    });
}

// Deposit refund
function OpenRefundInvoiceModal(Id) {
    $.ajax({
        url: "/Invoicings/DepositInvoices?InvoiceId=" + Id,
        method: "Get",
        success: function (data) {
            $('.emp').html('');
            var data = data.res;
            $("#InvoiceId").append(data.InvoiceId);
            $("#InvoiceCode").append(data.InvoiceCode);
            $("#InvoiceDate").append(data.InvoiceDate);
            $("#personId").append(data.personId);
            $("#InvoicePrice").append(data.InvoicePrice);
            $("#TaxAmount").append(data.TaxAmount);
            $("#NetAmount").append(data.NetAmount);
            $("#TaxIds").append(data.TaxIds);
            $("#Status").append(data.Status);
            $("#CreatedDate").append(data.CreatedDate);
            $("#UpdatedDate").append(data.UpdatedDate);
            $("#IsPaid").append(data.IsPaid);
            $("#InvoiceTypeId").append(data.InvoiceTypeId);
            $("#Refunded").append(data.Refunded);
            $("#ParentInvoiceId").append(data.ParentInvoiceId);
            $("#LocationId").append(data.LocationId);
            $("#TermID").append(data.TermID);
            $("#TotalDiscountAmount").append(data.TotalDiscountAmount);
            $("#CloneModal").modal();
            $('#form').find("textarea").val("");
        }
    });
}

function PostCloneInvoice() {
    if (!$('#invoiceCloneForm').valid()) {
        $('.error').addClass('text-danger');
        return;
    }
    $('.btn-ok').prop('disabled', true);
    $('.Customoverlay, .Customloader').show();
    $('#invoiceCloneForm').submit();
    $('#CloneModal').modal('hide');
}

// Transaction Password Modal Functions
function OpenTransactionPasswordModal() {
    $('#TransactionPasswordModal').modal();
    $('#TransactionPassword').val('');
    $('#passwordError').hide();
}

function ValidateTransactionPassword() {
    debugger
    var password = $('#TransactionPassword').val();
    var locationId = PartnerLedgerConfig.locationId;
    
    if (!password) {
        $('#passwordError').text('Please enter the transaction password').show();
        return;
    }
    
    $.ajax({
        url: '/Payments/ValidateTransactionPassword',
        type: 'POST',
        data: {
            password: password,
            locationId: locationId
        },
        success: function(response) {
            if (response.success) {
                isPasswordValidated = true; // Set security flag
                $('#TransactionPasswordModal').modal('hide');
                OpenPaymentModal();
            } else {
                $('#passwordError').text(response.message).show();
            }
        },
        error: function() {
            $('#passwordError').text('Error validating password. Please try again.').show();
        }
    });
}


