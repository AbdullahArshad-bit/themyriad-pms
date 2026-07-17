$(document).ready(function () {
    // Initialize multiselect for column visibility
    $('#columnvisibilty-filter').multiselect({
        nonSelectedText: 'Select columns to hide',
        maxHeight: 100,
        onChange: function (element, checked) {
            GetPlacements();
        },
    });

    // Initialize datetimepicker
    var startDate = new Date();
    $('.datetimepicker').datetimepicker({
        autoclose: true,
        format: 'dd-M-yyyy hh:ii:ss',
        todayBtn: true,
        showMeridian: true,
        startView: 2,
        startDate: startDate
    }).on('changeDate', function (selected) {
        startDate = new Date(selected.date.valueOf());
        $('.datetimepicker').datetimepicker('setStartDate', startDate);
    }).datetimepicker('setDate', startDate);

    // Initial table load
    GetPlacements();
});

var dataTable;
var PlacementId = 0;

function GetPlacements() {
    loadPlacementTable();

};

$('#inHouse').on('change', function () {
    if ($('#inHouse').is(':checked')) {
        $('#FromDate').val('');

    }
    else {
        var today = new Date();
        var fromDate = new Date(today.getFullYear() - 1, 8, 1);

        var monthNames = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
        var formatDate = '01/' + monthNames[fromDate.getMonth()] + '/' + fromDate.getFullYear();

        $('#FromDate').val(formatDate);

        $('#FromDate').datepicker('setDate', formatDate);

    }
    loadPlacementTable()
});

function loadPlacementTable() {

    ShowLoader();
    if (dataTable) {
        dataTable.destroy();
    }

    var SD = $('.from_date').val();
    var ED = $('.to_date').val();
    var id = $('#aud').val();
    var personId = $('#pid').val();
    var inHouse = $('#inHouse').is(':checked');
    var termID = $("#Terms").val();

    var urlParams = new URLSearchParams(window.location.search);
    var query = urlParams.get('queryby');
    if (inHouse == true) {
        query = "inHouse";
    }

    var actions = PlacementAction();
    var selectedColumns = $('#columnvisibilty-filter').val();
    var columns = [];
    if (selectedColumns) {
        selectedColumns.forEach(function (columnName) {
            var columnDef = {
                "data": columnName
            };
            columns.push(columnDef);
        });
    }

    dataTable = $("#tblGrid").DataTable({
        processing: false,
        serverSide: true,
        filter: true,
        paging: true,
        searching: true,
        lengthChange: true,
        scrollCollapse: true,
        scrollY: '70vh',
        scrollX: true,
        scroller: true,
        ordering: true,
        info: true,
        iDisplayLength: 100,
        lengthMenu: [
            [10, 25, 50, 100, 500, 1000, 1500],
            [10, 25, 50, 100, 500, 1000, 1500]
        ],
        autoWidth: true,
        responsive: true,
        columns: columns,
        language: {
            emptyTable: "No record found."
        },

        "ajax": {

            "url": "/BedSpacePlacement/PlacementsListAjax",
            "type": "POST",
            "data": function (d) {

                d.FromDate = SD;
                d.ToDate = ED;
                d.query = query;
                d.termID = termID;
                d.id = id;
                d.personId = personId;
                d.SelectedColumns = selectedColumns;

                var selectedColumn = $('#column-filter').val();
                if (selectedColumn) {
                    d.search = {
                        value: $('#tblGrid_filter input').val(),
                        column: selectedColumn
                    };
                }
                else {
                    d.search
                }

                var columnIndex = d.order[0].column;

                if (columnIndex === 0) {
                    d.orderBy = d.order[0].column;
                    d.orderDir = d.order[0].dir;
                } else {
                    var columnName = d.columns[columnIndex].data;
                    d.orderBy = columnName;
                    d.orderDir = d.order[0].dir;
                }

            },

            "datatype": "json",
            beforeSend: function () {
                ShowLoader();
            },
            complete: function () {
                HideLoader();
            },
            error: function (xhr, error, thrown) {
                console.error('AJAX error:', error, thrown);
                HideLoader();
            }
        },

        columns: [
            {
                "data": "PersonID",
                "orderable": false,    
                "searchable": false,   
                "visible": actions.includes("runAFeeAssessment-action"),
                "render": function (data, type, full, meta) {
                        return '<input type="checkbox" class="person-checkbox" value="' + data + '" style="cursor: pointer;"/>';
                },
            },

            {
                "data": "BedSpacePlacementID",
                "className": "menu-icons",
                render: function (data, type, full, meta) {
                    var availableActions = '';

                    if (actions.includes("edit-action"))
                        availableActions += `<a class = "edit-action" title="Edit Placement" href="/BedSpacePlacement/AddPlacements?bookingId=${full.BookingID}&placementId=${full.BedSpacePlacementID}">
                        <i class="fa fa-edit action-for-placement " ></i> </a>`

                    if (actions.includes("updatePlacementDate-action"))
                        availableActions += `<a class = "updatePlacementDate-action" title="Update Placement Dates" href="/BedSpacePlacement/EditPlacementDates?bookingId=${full.BookingID}&placementId=${full.BedSpacePlacementID}">
                        <i class="fa fa-calendar action-for-placement " ></i> </a>`

                    if (actions.includes("checkIn-action"))
                        availableActions += `<a class="checkIn-action checkInDisable" title="Check In" ${full.CheckIn === null ? 'onclick="OpenConfirmCheckInModal(' + full.BedSpacePlacementID + ',' + full.LocationID + ')" style="color:green"' : 'style="color:gray; cursor: not-allowed;"'}>
                        <i class="fa fa-arrow-circle-o-right action-for-placement"></i></a>`;

                    if (actions.includes("checkOut-action"))
                        availableActions += `<a class="checkOut-action ${full.CheckIn === null || full.CheckOut !== null ? 'checkOutDisable' : ''}" title="Check Out" ${full.CheckOut === null && full.CheckIn !== null ? 'onclick="OpenConfirmCheckoutModal(' + full.BedSpacePlacementID + ')" style="color:#fb900c"' : 'style="color:gray; cursor: not-allowed;"'}>
                        <i class="fa fa-arrow-circle-o-left action-for-placement"></i></a>`;

                    if (actions.includes("editProfile-action"))
                        availableActions += `<a class = "editProfile-action" title="Edit Profile" href="/PersonManage/AddPerson?id=${full.PersonID}">
                        <i class="fa fa-user action-for-placement"></i></a>`

                    if (actions.includes("inspection-action"))
                        availableActions += `<a class = "inspection-action" title="Inspection" ${'onclick="OpenConfirmInspectionModal(' + full.BedSpacePlacementID + ')" style="color:green;display:inline-block" '}>
                        <i style="cursor:pointer" class="fa fa-hand-pointer-o action-for-placement"></i></a>`

                    if (actions.includes("stdContract-action"))
                        availableActions += `<a class = "stdContract-action" title="Generate Contract" ${'onclick="GenerateContract(' + full.BedSpacePlacementID + ')"'}>
                        <i style="cursor:pointer" class="fa fa-file-word-o action-for-placement"></i></a>`

                    if (actions.includes("viewTransaction-action"))
                        availableActions += `<a class = "viewTransaction-action" title="View Transactions for ${full.FullName}" href="/payments/PartnerLedger?personId=${full.PersonID}">
                        <i class="fa fa-money action-for-placement"></i></a>`

                    if (actions.includes("roomSwap-action"))
                        availableActions += `<a class = "roomSwap-action" title="Room Swap for ${full.FullName}" href="/BedSpacePlacement/BedSpacePlacementMigration?PlacementId=${full.BedSpacePlacementID}">
                        <i class="fa fa-exchange action-for-placement"></i></a>`

                    if (actions.includes("viewFeedback-action"))
                        availableActions += `<a class = "viewFeedback-action" title="Feedback Rating" href="/Feedback/FeedbackSetting/ShowComment?personId=${full.PersonID}&placementid=${full.BedSpacePlacementID}">
                        <i class="fa fa-star action-for-placement"></i></a>`

                    if (actions.includes("reIssueCardlacement-action") && full.LocationID == 17) {
                        availableActions += `<a class="reIssueCardlacement-action ${full.CheckOut !== null ? 'reIssueCardDisable' : ''}" title="Re-Issue Card for ${full.FullName}" 
                        ${full.CheckOut === null ? 'onclick="OpenReissueCardModal(' + full.BedSpacePlacementID + ', ' + full.LocationID + ')" style="color:green"' : 'style="color:gray; cursor: not-allowed;"'}>
                        <i class="fa fa-refresh action-for-placement"></i></a>`;
                    }

                    if (actions.includes("deletePlacement-action"))
                        availableActions += `<a class = "deletePlacement-action" id="deleteForm" title="Delete" ${'onclick="OpenConfirmDeleteModal(' + full.BedSpacePlacementID + ')"'}" >
                        <i class="fa fa-trash action-for-placement"></i></a>`;

                    return availableActions;
                }
            },

            { "data": "LocationName", visible: !selectedColumns.includes("LocationName") },

            { "data": "Title", visible: !selectedColumns.includes("Title") },

            { "data": "FullName", visible: !selectedColumns.includes("FullName") },

            { "data": "Gender", visible: !selectedColumns.includes("Gender") },

            { "data": "Block", visible: !selectedColumns.includes("Block") },

            { "data": "BedSpace", visible: !selectedColumns.includes("BedSpace") },

            { "data": "Room", visible: !selectedColumns.includes("Room") },

            { "data": "RoomType", visible: !selectedColumns.includes("RoomType") },

            { "data": "Commitment", visible: !selectedColumns.includes("Commitment") },

            {
                "data": "Price",
                visible: !selectedColumns.includes("Price"),
                "className": "text-right",
                render: function (data, type, full, meta) {
                    return '<span>' + parseFloat(full.Price).toFixed(2) + '</span>'
                },

            },

            {
                "data": "BilledUpto",
                visible: !selectedColumns.includes("BilledUpto"),
                render: function (data, type, full, meta) {
                    if (data !== null && data !== undefined && data !== '') {
                        return '<span>' + movedateformate(data) + '</span>';
                    }
                    return '<span></span>';
                }
            },

            {
                "data": "MoveIn",
                visible: !selectedColumns.includes("MoveIn"),
                render: function (data, type, full, meta) {
                    return '<span>' + movedateformate(full.MoveIn) + '</span>'

                }
            },

            {
                "data": "MoveOut",
                visible: !selectedColumns.includes("MoveOut"),
                render: function (data, type, full, meta) {
                    return '<span>' + movedateformate(full.MoveOut) + '</span>'

                }
            },

            {
                "data": "CheckIn",
                visible: !selectedColumns.includes("CheckIn"),
                render: function (data, type, full, meta) {
                    return '<span>' + dateformate(full.CheckIn) + '</span>'
                },
            },

            {
                "data": "CheckOut",
                visible: !selectedColumns.includes("CheckOut"),
                render: function (data, type, full, meta) {
                    return '<span>' + dateformate(full.CheckOut) + '</span>'
                },
            },

            { "data": "Email", visible: !selectedColumns.includes("Email") },

            { "data": "Phone", visible: !selectedColumns.includes("Phone") },

            {
                "data": "DOB",
                visible: !selectedColumns.includes("DOB"),
                render: function (data, type, full, meta) {
                    return '<span>' + movedateformate(full.DOB) + '</span>'
                },
            },

            { "data": "University", visible: !selectedColumns.includes("University") },

            {
                "data": "Requests",
                visible: !selectedColumns.includes("Requests"),
                render: function (data, type, full, meta) {
                    var truncatedText = '';
                    var tooltipText = '';

                    if (data) {
                        truncatedText = data.split(' ').slice(0, 8).join(' ');
                        tooltipText = data.length > truncatedText.length ? data : '';

                        if (data.length > truncatedText.length) {
                            truncatedText += '....';
                        }
                    }
                    return `<span title="${tooltipText}">${truncatedText}</span>`;
                },
            }

        ],
        // Add callback to handle select all after data loads
        drawCallback: function (settings) {
            updateSelectAllCheckbox();
        }
    });
};
// Function to update the select all checkbox state
function updateSelectAllCheckbox() {
    var totalCheckboxes = $('#tblGrid tbody .person-checkbox').length;
    var checkedCheckboxes = $('#tblGrid tbody .person-checkbox:checked').length;

    var selectAllCheckbox = $('#select-all-checkbox');

    if (totalCheckboxes === 0) {
        selectAllCheckbox.prop('indeterminate', false);
        selectAllCheckbox.prop('checked', false);
    } else if (checkedCheckboxes === 0) {
        selectAllCheckbox.prop('indeterminate', false);
        selectAllCheckbox.prop('checked', false);
    } else if (checkedCheckboxes === totalCheckboxes) {
        selectAllCheckbox.prop('indeterminate', false);
        selectAllCheckbox.prop('checked', true);
    } else {
        selectAllCheckbox.prop('indeterminate', true);
        selectAllCheckbox.prop('checked', false);
    }
}

// Handle select all checkbox click
$(document).on('change', '#select-all-checkbox', function () {
    var isChecked = $(this).prop('checked');
    var currentPageCheckboxes = $('#tblGrid tbody .person-checkbox');

    currentPageCheckboxes.each(function () {
        var personId = $(this).val();
        var wasChecked = $(this).prop('checked');

        $(this).prop('checked', isChecked);

        // Update selectedPersonIds array
        if (isChecked && !wasChecked) {
            // Add to array if not already present
            if (!selectedPersonIds.includes(personId)) {
                selectedPersonIds.push(personId);
            }
        } else if (!isChecked && wasChecked) {
            // Remove from array
            selectedPersonIds = selectedPersonIds.filter(id => id !== personId);
        }
    });

    // Update button visibility
    if (selectedPersonIds.length === 0) {
        $('#generateInvoicesBtn').addClass("hide");
        $('#invoice-buttons-container').addClass("hide");
    } else {
        $('#generateInvoicesBtn').removeClass("hide");
        $('#invoice-buttons-container').removeClass("hide");
    }
});

var selectedPersonIds = [];

// Handle individual checkbox changes
$('#tblGrid tbody').on('change', '.person-checkbox', function () {
    var personId = $(this).val();

    if ($(this).prop('checked')) {
        if (!selectedPersonIds.includes(personId)) {
            selectedPersonIds.push(personId);
        }
    } else {
        selectedPersonIds = selectedPersonIds.filter(id => id !== personId);
    }

    if (selectedPersonIds.length === 0) {
        $('#generateInvoicesBtn').addClass("hide");
        $('#invoice-buttons-container').addClass("hide");
    } else {
        $('#generateInvoicesBtn').removeClass("hide");
        $('#invoice-buttons-container').removeClass("hide");
    }

    // Update select all checkbox state
    updateSelectAllCheckbox();
});

$('#generateInvoicesBtn').on('click', function () {
    $('#FeeAssesmentModal').modal('show');
});

function generateInvoices() {
    debugger
    var fromDate = $("#FromDates").val();
    var toDate = $("#ToDates").val();

    if (!fromDate) {
        showMessage("From Date is required.");
        return;
    }
    else if (!toDate) {
        showMessage("To Date Date is required.");
        return;
    }

    $('#FeeAssesmentModal').modal('hide');
    $('.Customoverlay, .Customloader').show();

    $.ajax({
        type: "POST",
        url: "/Invoicings/GenerateInvoices",
        data: {
            personIds: selectedPersonIds,
            StartDate: fromDate,
            EndDate: toDate
        },
        success: function (data) {
            $('.Customoverlay, .Customloader').hide();
            window.location.replace(GetFormatedUrl('/BedSpacePlacement/PlacementsList'));
        },
        error: function (data) {
            $('.Customoverlay, .Customloader').hide();
            $('.savebtn')
                .prop("disabled", false)
                .html('')
            showMessage('Some error occoured!');
        }
    });
}

function dateformate(date) {
    if (date == '' || date == null) {
        return '';
    } else {
        // let d = new Date(parseInt(date.substr(6))); parse json datetime to datetime
        // let _month = parseInt(d.getMonth()) + 1;
        // let _date = d.getDate() + "/" + _month + "/" + d.getFullYear();
        // let _time = d.toLocaleTimeString().toLowerCase().replace(/([\d]+:[\d]+):[\d]+(\s\w+)/g, "$1$2");
        // return _date + ' ' + _time;

        let d = new Date(parseInt(date.substr(6)));
        let _month, _date, _time;

        // if (window.location.hostname === 'testing' || window.location.hostname === '127.0.0.1') {
        //     _month = parseInt(d.getMonth()) + 1;
        if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
            _month = parseInt(d.getMonth()) + 1;
            _date = d.getDate() + "/" + _month + "/" + d.getFullYear();
            _time = d.toLocaleTimeString().toLowerCase().replace(/([\d]+:[\d]+):[\d]+(\s\w+)/g, "$1$2");
        }
        else {
            _month = d.getUTCMonth() + 1;
            _date = d.getUTCDate() + "/" + _month + "/" + d.getUTCFullYear();
            _time = d.toLocaleTimeString('en-US', { timeZone: 'UTC', hour: 'numeric', minute: 'numeric', hour12: true }).toLowerCase();
        }
        return _date + ' ' + _time;
    }
}

function movedateformate(date) {
    if (date == '' || date == null) {
        return '';
    } else {
        // let d = new Date(parseInt(date.substr(6)));
        // let _month = parseInt(d.getMonth()) + 1;
        // let _date = d.getDate() + "/" + _month + "/" + d.getFullYear();
        // return _date;
        
        let d = new Date(parseInt(date.substr(6)));
        let _month, _date;

        if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
            _month = parseInt(d.getMonth()) + 1;
            _date = d.getDate() + "/" + _month + "/" + d.getFullYear();
        }
        else {
            _month = d.getUTCMonth() + 1;
            _date = d.getUTCDate() + "/" + _month + "/" + d.getUTCFullYear();
        }
        return _date;
    }
}

function OpenReissueCardModal(id, locationId) {
    $('body .NewActionHiddenId').val(id);

    // Hide both fields initially
    $('#cardNumberContainerN').hide();
    $('#encoderNumberContainerN').hide();

    if (locationId == 16) {
        $('#cardNumberContainerN').show();
        $('#cardNumberN').prop('required', true);
        $('#encoderNumberN').prop('required', false);
    } else if (locationId == 17) {
        $('#encoderNumberContainerN').show();
        $('#encoderNumberN').prop('required', true);
        $('#cardNumberN').prop('required', false);
    }

    $('#ReIssueCardBtnConfirm').modal('show');

    $('#ReIssueCardBtnConfirm form').off('submit').on('submit', function (event) {
        event.preventDefault(); // Prevent the default form submission

        $('#ReIssueCardBtnConfirm .custom-spinner-container').show();
        $.ajax({
            type: "POST",
            url: $(this).attr('action'), // Get the form's action URL
            data: $(this).serialize(), // Serialize the form data
            success: function (data) {
                if (data.status == true) {
                    var table = $('#tblGrid').DataTable();
                    var $data = $(data);
                    for (var i = 0; i < $data.length; i++) {
                        if ($data[i] instanceof HTMLTableRowElement) {
                            table.row.add($data[i]);
                        }
                    }
                    table.draw();

                    showMessage(data.success);
                    $('#ReIssueCardBtnConfirm').modal('hide');
                    disableCheckInButton(id)
                        ;
                } else {
                    showMessage(data.error || 'Some error occurred!', 'error');
                }
            },
            error: function () {
                showMessage('Some error occurred!', 'error');
            },
            complete: function () {
                $('#ReIssueCardBtnConfirm .custom-spinner-container').hide();
            }
        });
    });
}

function OpenConfirmCheckInModal(id, locationId) {
    debugger
    $('body .CheckInHiddenId').val(id);

    // Hide both fields initially
    $('#cardNumberContainer').hide();
    $('#encoderNumberContainer').hide();

    if (locationId == 16) {
        $('#cardNumberContainer').hide();
        $('#cardNumber').prop('required', false);
        $('#encoderNumber').prop('required', false);
    } else if (locationId == 17) {
        $('#encoderNumberContainer').show();
        $('#encoderNumber').prop('required', true);
        $('#cardNumber').prop('required', false);
    }

    $('#CheckInBtnConfirm').modal('show');

    $('#CheckInBtnConfirm form').off('submit').on('submit', function (event) {
        event.preventDefault(); // Prevent the default form submission

        $('#CheckInBtnConfirm').modal('hide');
        $('.Customoverlay, .Customloader').show();

        $.ajax({
            type: "POST",
            url: $(this).attr('action'), // Get the form's action URL
            data: $(this).serialize(), // Serialize the form data
            success: function (data) {
                if (data.status == true) {
                    var table = $('#tblGrid').DataTable();
                    var $data = $(data);
                    for (var i = 0; i < $data.length; i++) {
                        if ($data[i] instanceof HTMLTableRowElement) {
                            table.row.add($data[i]);
                        }
                    }
                    table.draw();
                    $('.Customoverlay, .Customloader').hide();
                    showMessage(data.success);
                    disableCheckInButton(id);

                } else {
                    showMessage(data.error || 'Some error occurred!', 'error');
                }
            },
            error: function () {
                $('.Customoverlay, .Customloader').hide();
                showMessage('Some error occurred!', 'error');
            }
        });
    });
}

function disableCheckInButton(id) {

    var table = $('#tblGrid').DataTable();

    table.rows().every(function () {
        var rowData = this.data();
        if (rowData.BedSpacePlacementID === id) {
            var $checkInButton = $(this.node()).find('.checkIn-action');
            var $checkOutButton = $(this.node()).find('.checkOut-action');

            $checkInButton.css('color', 'gray');
            $checkInButton.off('click');
            $checkInButton.removeAttr('onclick');
            $checkInButton.addClass('checkInDisable');

            $checkOutButton.css('color', '#fb900c');
            $checkOutButton.attr('onclick', 'OpenConfirmCheckoutModal(' + id + ')');
            $checkOutButton.removeClass('checkOutDisable');
        }
    });
}

function OpenConfirmCheckoutModal(id) {

    $('body .CheckOutHiddenId').val(id);
    $('#CheckOutBtnConfirm').modal('show');

    $('#CheckOutBtnConfirm form').off('submit').on('submit', function (event) {
        event.preventDefault(); // Prevent the default form submission

        $('#CheckOutBtnConfirm .custom-spinner-container').show();

        $.ajax({
            type: "POST",
            url: $(this).attr('action'), // Get the form's action URL
            data: $(this).serialize(), // Serialize the form data
            success: function (data) {
                if (data.status == true) {
                    var table = $('#tblGrid').DataTable();
                    var $data = $(data);
                    for (var i = 0; i < $data.length; i++) {
                        if ($data[i] instanceof HTMLTableRowElement) {
                            table.row.add($data[i]);
                        }
                    }
                    table.draw();
                    showMessage(data.success);
                    $('#CheckOutBtnConfirm').modal('hide');
                    disableCheckOutButton(id); // Call the function to disable the button for the bed space placement ID
                } else {
                    showMessage(data.error || 'Some error occurred!', 'error');
                }
            },
            error: function () {
                showMessage('Some error occurred!', 'error');
            },
            complete: function () {
                $('#CheckOutBtnConfirm .custom-spinner-container').hide();
            }
        });
    });
}

function disableCheckOutButton(id) {
    var table = $('#tblGrid').DataTable();

    table.rows().every(function () {
        var rowData = this.data();

        if (rowData.BedSpacePlacementID === id) {
            var $checkOutButton = $(this.node()).find('.checkOut-action');
            $checkOutButton.css('color', 'gray');
            $checkOutButton.off('click');
            $checkOutButton.removeAttr('onclick');
            $checkOutButton.addClass('checkOutDisable');
        }
    });
}

var bedSpaceID = "";
function OpenConfirmInspectionModal(id) {
    bedSpaceID = id;
    $('#InspectionConfirm').modal('show');
}

var id = "";
function OpenConfirmDeleteModal(ID) {
    id = ID;
    $('#confirm-delete').modal('show');
}

function DeletePlacement() {
    $.ajax({
        url: GetFormatedUrl("/BedSpacePlacement/DeletePlacement?id=" + id),
        type: "POST",
        dataType: "json",
        contentType: "application/json;charset=UTF-8",
        success: function (data) {

            if (data.Success != 'true' && data.Code == 404) {
                return;
            }

            if (data.success != 'false') {
                window.location.href = GetFormatedUrl("/BedSpacePlacement/PlacementsList");
            }

            else {
                Toast.show(data.message, "error");
            }
        }
    })
}

function GenerateContract(id) {
    PlacementId = id;
    // Open modal immediately to improve perceived performance
    try {
        $('#ContractId').attr('disabled', true).attr('readonly', true);
        $('#GrossRent').val('Loading...').attr('readonly', true);
        $('#NetAmount').val('Loading...').attr('readonly', true);
        $('#RegistrationFee').val('Loading...').attr('readonly', true);
        $('#SecurityFee').val('Loading...').attr('readonly', true);
        $('#GenerateContractModel').modal('show');
    } catch (e) { }

    $.ajax({
        url: "/ContractsManage/GetContractDetailByPlacement?PlacementId=" + id,
        method: "Get",
        success: function (data) {

            if (data.ContractId != 0 && data.IsPublish == true) {
                $('#ContractId').val(data.ContractId);
                $('#ContractId').attr('disabled', true).attr("readonly", true);
            }
            if (data.ContractId == 0 && data.IsPublish == false) {
                $('#ContractId').attr('disabled', false).attr("readonly", false);
                $('#ContractId').val('');
            }
            if (data.ContractId != 0 && data.IsPublish == false) {
                $('#ContractId').attr('disabled', false).attr("readonly", false);
                $('#ContractId').val('');
            }

            $('#GrossRent').val(data.GrossAmount).attr('readonly', true);
            $('#NetAmount').val(data.NetAmount).attr('readonly', true);
            $("#RegistrationFee").val(0).attr('readonly', true);
            $('#SecurityFee').val(data.SecurityDeposit).attr('readonly', true);
            var LocationId = localStorage.getItem("selectedLocation");
            if (LocationId == 17) {
                $('#GrossRent').attr('readonly', false);
                $('#SecurityFee').attr('readonly', false);
                $('#RegistrationFee').attr('readonly', false);
                $('#NetAmount').attr('readonly', false);
            }
        },
        error: function () {
            // Keep modal open with editable fields if prefill fails
            try {
                $('#ContractId').attr('disabled', false).attr('readonly', false).val('');
                $('#GrossRent').val('').attr('readonly', false);
                $('#NetAmount').val('').attr('readonly', false);
                $('#RegistrationFee').val('').attr('readonly', false);
                $('#SecurityFee').val('').attr('readonly', false);
                if (typeof Toast !== 'undefined') {
                    Toast.show('Could not prefill contract data. You can still proceed.', 'error');
                }
            } catch (e) { }
        }
    });
}


$('#GenerateContractModelForm').validate({ // initialize the plugin
    rules: {
        ContractId: {
            required: true,
        },
        GrossRent: {
            required: true,
        },
        NetAmount: {
            required: true,
        },
        TaxAmount: {
            required: true,
        },
        RegistrationFee: {
            required: true,
        },
        SecurityFee: {
            required: true,
        },
        DiscountAmount: {
            required: true,
        },
    }
});

function PostGenerateContract() {
    var ContractId = $('#ContractId').val();
    var GrossAmount = $('#GrossRent').val();
    var NetAmount = $('#NetAmount').val();
    var TaxAmount = 0;
    var RegistrationFee = $('#RegistrationFee').val();
    var SecurityFee = $('#SecurityFee').val();
    var DiscountAmount = 0;
    if (!$('#GenerateContractModelForm').valid()) {
        $('.error').addClass('text-danger');
        return;
    }
    var data = {
        PlacementId: PlacementId,
        ContractId: ContractId,
        GrossAmount: GrossAmount,
        DiscountAmount: DiscountAmount,
        NetAmount: NetAmount,
        TaxAmount: TaxAmount,
        RegistrationFee: RegistrationFee,
        SecurityDeposit: SecurityFee
    }

    $('#GenerateContractModel').modal('hide');
    $('.Customoverlay, .Customloader').show();

    $.ajax({

        type: "Post",
        url: GetFormatedUrl('/ContractsManage/GenerateStudentContract'),
        data: {
            studentConractsVM: data
        },
        success: function (response) {
            $('.Customoverlay, .Customloader').hide();
            if (response.status == true)
                Toast.show(response.message, "success");
        },
        error: function (data) {
            $('.Customoverlay, .Customloader').hide();
            $('.savebtn')
                .prop("disabled", false)
                .html('')
            showMessage('Some error occoured!');
        }
    });
}

function GenerateInspection() {
    var obj = new Object();

    obj.BedSpaceID = bedSpaceID;
    obj.InspectionID = $('#Inspection').val();
    obj.Remarks = $('#Remarks').val();
    var data = JSON.stringify(obj);

    if (!$('#InspectionConfirmForm').valid()) {
        $('.error').addClass('text-danger');
        return;
    }

    $.ajax({
        url: GetFormatedUrl("/Inspection/GenerateNewInspection"),
        type: "POST",
        data: data,
        dataType: "json",
        contentType: "application/json;charset=UTF-8",
        success: function (data) {
            if (data.status != 'true' && data.Code == 404) {
                Toast.show("Unable to process your request. Pleae try again later.", "error");
                return;
            }
            if (data.status != 'false' && data.Code == 200) {
                window.location.href = GetFormatedUrl("/Inspection/GeneratedInspections");
                Toast.show(data.data, "success");
            }
        }
    })
}


$('#InspectionConfirmForm').validate({ // initialize the plugin
    rules: {
        Inspection: {
            required: true,
        }
    }
});

var startDate = new Date();
var FromEndDate = new Date();
var ToEndDate = new Date();
ToEndDate.setDate(ToEndDate.getDate() + 365);

// Initialize from date picker
$('.assessment_from_dates').datepicker({
    weekStart: 1,
    startDate: '',
    format: "dd/M/yyyy",
    orientation: "bottom left",
    autoclose: true
}).on('changeDate', function (e) {
    var selectedFromDate = e.date;

    // Set minDate for to date picker based on selected from date
    $('.assessment_to_dates').datepicker('setStartDate', selectedFromDate);
    $('.assessment_to_dates').datepicker('setDate', selectedFromDate); // Optional: pre-fill to date with from date
});

// Initialize to date picker
$('.assessment_to_dates').datepicker({
    weekStart: 1,
    startDate: '', // will be dynamically set after from_date is selected
    format: "dd/M/yyyy",
    orientation: "bottom left",
    autoclose: true
});

$('.from_date').datepicker({
    weekStart: 1,
    startDate: '',
    format: "dd/M/yyyy",
    orientation: "bottom left",

    autoclose: true
})

    .on('changeDate', function (selected) {
        startDate = new Date(selected.date.valueOf());
        $('.to_date').datepicker('setStartDate', startDate);
    });

$('.to_date')
    .datepicker({
        weekStart: 1,
        startDate: new Date($('.from_date').val()),
        endDate: ToEndDate,
        format: "dd/M/yyyy",
        orientation: "bottom left",
        autoclose: true,

    })

    .on('changeDate', function (selected) {
        FromEndDate = new Date(selected.date.valueOf());
        FromEndDate.setDate(FromEndDate.getDate(new Date(selected.date.valueOf())));
        $('.from_date').datepicker('setEndDate', FromEndDate);
    });

function GetExcel() {
    var SD = $('.from_date').val();
    var ED = $('.to_date').val();
    var inHouse = $('#inHouse').is(':checked');
    var termID = $("#Terms").val();
    var urlParams = new URLSearchParams(window.location.search);
    var query = urlParams.get('queryby');
    window.location.href = GetFormatedUrl("/BedSpacePlacement/ExportPlacementReport?FromDate=" + SD + "&ToDate=" + ED + "&inHouse=" + inHouse + "&termID=" + termID + "&query=" + query);
}