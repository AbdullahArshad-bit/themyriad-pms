// Invoicing Index page script.
// Razor-supplied values are read from window.invoicingIndexConfig set in Index.cshtml.
(function () {
    var cfg = (window && window.invoicingIndexConfig) ? window.invoicingIndexConfig : {};

    var dataTable;
    $(document).ready(function () {
        $('#columnvisibilty-filter').multiselect({

            nonSelectedText: 'Select columns to hide',
            maxHeight: 100,
            onChange: function (element, checked) {

                GetInvoices();
            },
        });
        GetInvoices();
    });

    function InvoiceAction() {

        var actions = new Array();
        if (cfg.editAction) actions.push("edit-action");
        if (cfg.approveAction) actions.push("approve-action");
        if (cfg.editAfterApproveAction) actions.push("editAfterApprove-action");
        if (cfg.invDetAction) actions.push("detail-action");
        if (cfg.reverseInvoiceAction) actions.push("reverseInvoice-action");
        if (cfg.invoiceVoucherAction) actions.push("invoiceVoucher-action");
        if (cfg.paymentLinkAction) actions.push("paymentLink-action");
        return actions;
    }

    function GetInvoices() {

        loadInvoicesTable();
    };

    function loadInvoicesTable() {
        ShowLoader();

        if (dataTable) {
            dataTable.destroy();
        }
        var SD = $('.from_date').val();
        var ED = $('.to_date').val();
        var invoiceTypeId = $('#InvoiceTypeId').val();

        var urlParams = new URLSearchParams(window.location.search);
        var query = urlParams.get('queryby');

        var actions = InvoiceAction();
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
        dataTable = $('#tblGrid').DataTable({
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
            autoWidth: true,
            responsive: true,
            columns: columns,
            language: {
                emptyTable: "No record found.",
            },
            "ajax": {
                "url": "/Invoicings/loadInvoicingbyAjax",
                "type": "POST",
                "data": function (d) {
                    d.FromDate = SD;
                    d.ToDate = ED;
                    d.InvoiceTypeId = invoiceTypeId;
                    d.query = query;
                    d.SelectedColumns = selectedColumns;

                    var selectedColumn = $('#column-filter').val();
                    if (selectedColumn) {
                        d.search = {
                            value: $('#tblGrid_filter input').val(), // Include the search query
                            column: selectedColumn // Include the selected column
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
                    ShowLoader(); // Show loader before AJAX request
                },
                complete: function () {
                    HideLoader(); // Hide loader after AJAX request
                },
                error: function (xhr, error, thrown) {
                    console.error('AJAX error:', error, thrown);
                    HideLoader();
                }
            },

            columns: [
                {
                    "data": "id",
                    render: function (data, type, full, meta) {
                        var isDubai = full.LocationId == cfg.dubaiLocationId;
                        var isMuscat = full.LocationId == cfg.muscatLocationId;
                        var availableActions = '';
                        if (full.Status != true) {
                            if (actions.includes("edit-action"))
                                availableActions += `<a class = "edit-action EditHideAfterApproval menu-icons" style="text-align:inherit" title="Edit Invoice" href="/Invoicings/Edit?id=${full.Id}">
                <i class="fa fa-edit action-for-Invoice"></i></a>`
                        }

                        if (full.Status != true) {

                            if (actions.includes("approve-action"))
                                availableActions += `<a class = "approve-action ApproveHideAfterApproval menu-icons" style="text-align:inherit" href="#Approve" title="Approve" ${'onclick="ApproveConform(' + full.Id + ')"'}>
                <i class="fa fa-thumbs-up action-for-Invoice"></i></a>`
                        }

                        if (full.Status == true && (full.Refunded != true)) {
                            if (actions.includes("editAfterApprove-action"))
                                availableActions += `<a class = "editAfterApprove-action menu-icons" style="text-align:inherit" title="Edit After Approved" href="/Invoicings/Edit?id=${full.Id}">
                <i class="fa fa-edit action-for-Invoice"></i></a>`
                        }

                        if (actions.includes("detail-action"))
                            availableActions += `<a class = "detail-action menu-icons" style="text-align:inherit" title="Details" href="/Invoicings/Details?id=${full.Id}">
                <i class="fa fa-file-text-o action-for-Invoice"></i></a>`


                        if (full.VoucherId != 0) {
                            if (actions.includes("invoiceVoucher-action"))
                                availableActions += `<a class="voucher-action menu-icons" style="text-align:inherit" title="Voucher Details" href="/Voucher/InvoicingVoucher?id=${full.VoucherId}">
                <i class="fa fa-ticket action-for-Voucher"></i></a>`
                        }

                        if (full.isPaid != true
                            && full.Status == true
                            && (full.LocationId === cfg.muscatLocationId || full.LocationId === cfg.dubaiLocationId)
                            && (full.Refunded == false || full.Refunded == null)) {
                            if (actions.includes("paymentLink-action"))
                                availableActions += `<a class = "approve-action menu-icons" style="text-align:inherit" title="Payment Link"  ${'onclick="javascript:ConfirmPayment(' + full.Id + ',' + full.LocationId + ')"'}>
                <i class="fa fa-credit-card action-for-Invoice"></i></a>`
                        }

                        if (actions.includes("detail-action"))
                            availableActions += `<a class = "approve-action menu-icons" style="text-align:inherit" title="Print" href = "javascript: w=window.open('/Invoicings/Details?id=${full.Id}&prnt=1');" >
                <i class="fa fa-print action-for-Invoice"></i></a>`

                        if (isDubai && full.InvoiceTypeId == cfg.miscellaneousInvoiceTypeId && full.Status == true && full.ParentInvoiceId == null && full.Refunded == null) {
                            if (actions.includes("reverseInvoice-action"))
                                availableActions += `<a class="reverseInvoice-action menu-icons" title="Reverse Invoice" onclick="OpenConfirmReverseInvoiceModalForMuscat(${full.Id}, ${full.LocationId}, ${full.InvoiceTypeId})">
                <i class="fa fa-level-down action-for-Invoice"></i></a>`;
                        }

                        else if (isDubai && full.InvoiceTypeId != cfg.depositInvoiceTypeId && full.Status == true && full.ParentInvoiceId == null && full.Refunded == null) {
                            if (actions.includes("reverseInvoice-action"))
                                availableActions += `<a class="reverseInvoice-action menu-icons" style="text-align:inherit" title="Reverse Invoice" onclick="OpenConfirmReverseInvoiceModal(${full.Id}, ${full.LocationId}, ${full.InvoiceTypeId}, ${full.StudentId})">
                <i class="fa fa-level-down action-for-Invoice"></i></a>`;
                        }
                        else if (isMuscat && full.isPaid != true && full.InvoiceTypeId != cfg.depositInvoiceTypeId && full.Status == true
                            && full.NetAmount == full.PendingBalance && full.ParentInvoiceId == null && full.Refunded == null) {
                            if (actions.includes("reverseInvoice-action"))
                                availableActions += `<a class="reverseInvoice-action menu-icons" style="text-align:inherit" title="Reverse Invoice" onclick="OpenConfirmReverseInvoiceModalForMuscat(${full.Id}, ${full.LocationId}, ${full.InvoiceTypeId})">
                <i class="fa fa-level-down action-for-Invoice"></i></a>`;
                        }
                        return availableActions;
                    },

                },
                { "data": "Location", visible: !selectedColumns.includes("Location") },
                { "data": "Code", visible: !selectedColumns.includes("Code") },
                { "data": "MyriadID", visible: !selectedColumns.includes("MyriadID") },
                { "data": "FullName", visible: !selectedColumns.includes("FullName") },
                {
                    "data": "InvoiceDate",
                    "className": "text-left",
                    "visible": !selectedColumns.includes("InvoiceDate"),
                    render: function (data, type, full, meta) {
                        return '<span>' + dateformate(full.InvoiceDate) + '</span>'
                    },
                },
                {
                    "data": "FromDate",
                    "className": "text-left",
                    "visible": !selectedColumns.includes("FromDate"),
                    render: function (data, type, full, meta) {
                        return '<span>' + dateformate(full.FromDate) + '</span>'
                    },
                },
                {
                    "data": "ToDate",
                    "className": "text-right",
                    "visible": !selectedColumns.includes("ToDate"),
                    render: function (data, type, full, meta) {
                        return '<span>' + dateformate(full.ToDate) + '</span>'
                    },
                },
                { "data": "Remarks", visible: !selectedColumns.includes("Remarks") },
                { "data": "ServiceName", visible: !selectedColumns.includes("ServiceName") },

                {
                    "data": "NetAmount",
                    "className": "text-right",
                    "visible": !selectedColumns.includes("NetAmount"),
                    render: function (data, type, full, meta) {
                        return '<span>' + parseFloat(full.NetAmount).toFixed(2) + '</span>'
                    },
                },

                {
                    "data": "PendingBalance",
                    "className": "text-right",
                    "visible": !selectedColumns.includes("PendingBalance"),
                    render: function (data, type, full, meta) {
                        var pendingBalance = full.PendingBalance || 0;
                        if (pendingBalance == 0 || pendingBalance < 0) {
                            return '<span>' + parseFloat(pendingBalance).toFixed(2) + '</span>';
                        }
                        else if (pendingBalance > 0 && full.Refunded == true) {
                            return '<span>' + parseFloat(pendingBalance).toFixed(2) + '</span>';
                        }
                        else {
                            return '<span class="text-danger label label-danger">' + parseFloat(pendingBalance).toFixed(2) + '</span>';

                        }
                    },
                },
                {
                    "data": "TotalBalanceOfResident",
                    className: "text-right",
                    visible: !selectedColumns.includes("TotalBalanceOfResident"),
                    render: function (data, type, full, meta) {
                        var totalBalanceOfResident = full.TotalBalanceOfResident || 0;
                        if (totalBalanceOfResident == 0 || totalBalanceOfResident < 0) {
                            return '<span>' + parseFloat(totalBalanceOfResident).toFixed(2) + '</span>';
                        }
                        else if (totalBalanceOfResident > 0 && full.Refunded == true) {
                            return '<span>' + parseFloat(totalBalanceOfResident).toFixed(2) + '</span>';
                        }
                        else {
                            return '<span class="text-danger label label-danger">' + parseFloat(totalBalanceOfResident).toFixed(2) + '</span>';
                        }
                    },
                },
                {
                    "data": "Status",
                    visible: !selectedColumns.includes("Status"),
                    render: function (data, type, full, meta) {
                        if (full.Status == true) {
                            return `<span class="text-success label label-success">Approved</span>`
                        }
                        else {
                            return `<span class="text-danger label label-primary ToBeApproved-${full.Id}">Pending</span>`
                        }
                    },
                },
                {
                    "data": "IsPaid",
                    visible: !selectedColumns.includes("IsPaid"),
                    render: function (data, type, full, meta) {
                        if (full.isPaid == true && full.InvoiceTypeId == cfg.depositInvoiceTypeId && full.Refunded == true) {
                            return `<span class="text-danger label label-danger">Refunded</span>`;
                        }
                        else if (full.isPaid == true) {
                            return `<span class="text-success label label-success">Paid</span>`
                        }
                        else if (full.Status == true && (full.ParentInvoiceId == null || full.ParentInvoiceId != null) && full.Refunded == true) {
                            return `<span class="text-danger label label-danger">Reversed</span>`;
                        }
                        else {
                            return `<span class="text-danger label label-primary ToBeApproved-${full.Id}">Unpaid</span>`
                        }
                    },
                },
                { "data": "CreatedBy", visible: !selectedColumns.includes("CreatedBy") },
                { "data": "ApprovedBy", visible: !selectedColumns.includes("ApprovedBy") },
                {
                    "data": "CreatedDate",
                    visible: !selectedColumns.includes("CreatedDate"),
                    "className": "text-left",
                    "visible": !selectedColumns.includes("CreatedDate"),
                    render: function (data, type, full, meta) {
                        return '<span>' + dateformate(full.CreatedDate) + '</span>'
                    },
                }
            ],


        });
    };

    function GetTaxes() {
        $.ajax({
            type: "Get",
            url: GetFormatedUrl("/Invoicings/GetTaxes"),
            async: false,
            success: function (response) {
                var taxes = response.data;
                $('.taxes').each(function () {
                    var $select = $(this);
                    var selectedTaxIds = $select.data('selected-taxes');
                    var selectedArray = [];
                    if (selectedTaxIds !== undefined && selectedTaxIds !== null) {
                        selectedArray = selectedTaxIds.toString().split(',');
                    }

                    $select.empty();

                    var html = "";
                    $.each(taxes, function (key, value) {
                        html += `<option Value='${value.Value}'>${value.Text}</option>`;
                    });

                    $select.html(html);

                    if (selectedArray.length > 0) {
                        $select.val(selectedArray).trigger('change');
                    }

                    $select.select2();
                });

            }
        });
    }

    function GetTaxPercentage(Id) {
        var value = 0;
        $.ajax({
            type: "Get",
            async: false,
            url: GetFormatedUrl('/Invoicings/GetTaxPercenageValueById/') + Id,
            success: function (data) {
                var Data = data.status;
                value = data.Value;
            },
            error: function (data) {
            }
        });
        return value;
    }

    var reverseCalcPendingCount = 0;

    function beginReverseCalculation() {
        reverseCalcPendingCount++;
        setReverseCalculationBusy(true);
    }

    function endReverseCalculation() {
        if (reverseCalcPendingCount > 0) {
            reverseCalcPendingCount--;
        }
        if (reverseCalcPendingCount === 0) {
            setReverseCalculationBusy(false);
        }
    }

    function setReverseCalculationBusy(busy) {
        var $modal = $('#ReverseInvoiceBtnConfirm');
        if (busy) {
            $modal.find('.reverse-calc-overlay').addClass('active');
            $modal.find('.btn-ok').prop('disabled', true);
        } else {
            $modal.find('.reverse-calc-overlay').removeClass('active');
            updateReverseSubmitButtonState();
        }
    }

    function updateReverseSubmitButtonState() {
        var allValid = true;
        $('#reverseInvoiceTableBody tr').each(function () {
            if ($(this).hasClass('danger')) {
                allValid = false;
            }
        });
        var remarksValid = $.trim($('#reverseInvoiceRemarks').val()).length > 0;
        var canSubmit = allValid && remarksValid && reverseCalcPendingCount === 0;
        $('#ReverseInvoiceBtnConfirm .btn-ok').prop('disabled', !canSubmit);
    }

    function getServiceTypeId(invoiceDetailId, $row) {
        if (!invoiceDetailId) {
            console.error('No invoice ID provided');
            return;
        }

        beginReverseCalculation();

        $.ajax({
            url: GetFormatedUrl('/Invoicings/GetServiceTypeId'),
            type: 'GET',
            data: {
                invoiceDetailId: invoiceDetailId
            },
            success: function (response) {
                if (response.success) {
                    var serviceTypeId = response.data;
                    $row.data('service-type-id', serviceTypeId);
                    GetResidentOccupancy(serviceTypeId, $row);
                } else {
                    console.error('Failed to fetch invoice types:', response.message);
                    endReverseCalculation();
                }
            },
            error: function (xhr, status, error) {
                console.error('API call failed:', error);
                endReverseCalculation();
            }
        });
    }

    function GetResidentOccupancy(serviceTypeId, $row) {
        const studentId = $("#StudentId").val();
        const invoiceTypeId = $("#InvoiceTypeId").val();
        const serviceId = $row.data("service-id");
        const taxId = $row.data("tax-id");

        const rentalInvoiceTypeId = cfg.rentalInvoiceTypeId;
        const miscellaneousInvoiceTypeId = cfg.miscellaneousInvoiceTypeId;
        const isRentalInvoice = parseInt(invoiceTypeId) === rentalInvoiceTypeId;
        const isPackageService = (parseInt(invoiceTypeId) === miscellaneousInvoiceTypeId && serviceTypeId == 3);

        const url = isPackageService
            ? GetFormatedUrl('/invoicings/ResidentPackage')
            : GetFormatedUrl('/invoicings/ResidentOccupancy');

        const data = {
            studentId,
            serviceId,
            invoiceTypeId,
            taxId
        };

        $.ajax({
            url,
            method: "GET",
            data,
            success: function (response) {
                if (response.status && response.data) {
                    var res = response.data;
                    $("#OccupancyId").val(res.OccupancyId);
                    GetFrequency($row);
                } else {
                    endReverseCalculation();
                }
            },
            error: function (xhr, status, error) {
                showMessage("An error occurred while fetching occupancy data.");
                HideLoader();
                endReverseCalculation();
            }
        });
    }

    function GetFrequency($row) {
        var studentId = $("#StudentId").val();
        var configId = $("#PriceConfig").val();

        if (configId == null || configId == "" || configId == "0" || configId == undefined) {
            var configId = $("#OccupancyId").val();
        }

        $.ajax({
            url: GetFormatedUrl('/invoicings/GetFrequencyById?Id=' + studentId + "&configId=" + configId),
            method: "GET",
            success: function (res) {

                if (res && res.status && res.data) {
                    $("#FrequencyId").val(res.data.FrequencyId);
                }
                else {
                    $("#FrequencyId").val("");
                }
                validateRowDates($row);
                var nextRow = $row.next('tr');
                if (nextRow.length && nextRow.data('service-name')?.includes('cleaning')) {

                    var fromDate = $row.find('.from-date-reverse').val();
                    var toDate = $row.find('.to-date-reverse').val();

                    recalculateCleaningAmount(nextRow, fromDate, toDate);
                }
                calculateReverseTotals();
                endReverseCalculation();
            },
            error: function () {
                console.error("Failed to fetch frequency.");
                endReverseCalculation();
            }
        });
    }

    function OpenConfirmReverseInvoiceModal(id, locationId, invoiceTypeId, studentId) {
        $('body #ReveserInvoiceHiddenId').val(id);
        $("#InvoiceTypeId").val(invoiceTypeId);
        $("#StudentId").val(studentId);
        $('#reverseInvoiceRemarks').val('');
        $('#ReverseInvoiceBtnConfirm .btn-ok').prop('disabled', true);
        ShowLoader();

        $.ajax({
            url: GetFormatedUrl('/Invoicings/GetReverseInvoiceData'),
            type: 'GET',
            data: { id: id },
            success: function (response) {
                if (response.success) {
                    var data = response.data;

                    if (data.length > 0) {
                        $('#PriceConfig').val(data[0].PriceConfig);
                    }

                    var tableBody = $('#reverseInvoiceTableBody');
                    tableBody.empty();

                    data.forEach(function (detail, index) {
                        var fromDate = formatDateForDatepicker(detail.FromDate);
                        var toDate = formatDateForDatepicker(detail.ToDate);
                        var originalDays = getDaysBetween(detail.FromDate, detail.ToDate);
                        var serviceName = detail.ServiceName.toLowerCase();

                        var row = `
                            <tr class="reverse-service-row" data-id="${detail.Id}" data-invoicing-id="${detail.InvvoicingId}" data-service-id="${detail.ServiceId}" data-tax-id="${detail.TaxesIds}" data-original-from="${fromDate}"
                            data-original-to="${toDate}" data-original-days="${originalDays}" data-original-price="${detail.Price}" data-base-service-price="${detail.BaseServicePrice}" data-service-name="${serviceName}">
                                <td>${detail.ServiceName}</td>
                                <td>${detail.Description || ''}</td>
                                <td>
                                    <input type="text" class="form-control from-date-reverse" data-original-from="${fromDate}" data-original-to="${toDate}"
                                           value="${fromDate}" style="width: 120px;" autocomplete="off" ${serviceName.includes('cleaning') ? 'readonly disabled' : ''}>
                                </td>
                                <td>
                                    <input type="text" class="form-control to-date-reverse" data-original-from="${fromDate}" data-original-to="${toDate}"
                                           value="${toDate}" style="width: 120px;" autocomplete="off" ${serviceName.includes('cleaning') ? 'readonly disabled' : ''}>
                                </td>
                                <td>
                                    <input type="text" value="${detail.Price.toFixed(2)}"
                                           class="form-control calculated-amount" style="width: 120px;" readonly>
                                </td>
                                <td>
                                    <select autocomplete="off" class="form-control select2 taxes" value="${detail.TaxesIds}" data-selected-taxes="${detail.TaxesIds}" data-tax-amount="${detail.TaxesAmount}"
                                    multiple="multiple" style="width: 120px;" ${serviceName.includes('cleaning') ? 'readonly disabled' : ''}></select>
                                </td>
                                 <td>
                                    <input type="text" value="${detail.DiscountPercentage || 0}" data-discount-amount="${detail.DiscountAmount}"
                                           class="form-control discount-percentage" style="width: 120px;" readonly disabled}>
                                </td>
                                <td>
                                     <input type="text" value="${(detail.TotalAmount).toFixed(2)}" class="form-control net-total-amount" style="width: 120px;" readonly>
                                </td>
                            </tr>
                        `;
                        tableBody.append(row);
                    });

                    initializeReverseDatepickers();
                    initializeReverseDateSyncHandlers();
                    GetTaxes();
                    calculateReverseTotals();
                    updateReverseSubmitButtonState();

                    $('#ReverseInvoiceBtnConfirm').modal('show');
                    $('#ReverseInvoiceBtnConfirm form').off('submit').on('submit', function (event) {
                        event.preventDefault();
                        submitReverseInvoice(locationId);
                    });
                } else {
                    showMessage(response.message || 'Failed to load invoice data');
                }
            },
            error: function () {
                console.error('Failed to load invoice data');
            },
            complete: function () {
                HideLoader();
            }
        });
    }

    function calculateReverseTotals() {
        var subtotal = 0;
        var taxTotal = 0;
        var discountTotal = 0;
        var netTotal = 0;

        $("#reverseInvoiceTableBody tr").each(function () {
            var $row = $(this);

            var calculatedAmount = parseFloat($row.find(".calculated-amount").val()) || 0;
            var taxAmount = parseFloat($row.find(".taxes").data("tax-amount")) || 0;
            var discountAmount = parseFloat($row.find(".discount-percentage").data("discount-amount")) || 0;
            subtotal += calculatedAmount;
            taxTotal += taxAmount;
            discountTotal += discountAmount;
        });

        netTotal = subtotal + taxTotal - discountTotal;
        $("#subTotalAmount").text(subtotal.toFixed(2));
        $("#taxAmount").text(taxTotal.toFixed(2));
        $("#discountAmount").text(discountTotal.toFixed(2));
        $("#NetTotalAmount").text(netTotal.toFixed(2));

        $('#SubtotalAmountHiddenId').val(subtotal.toFixed(2));
        $('#TaxtotalAmountHiddenId').val(taxTotal.toFixed(2));
        $('#DiscounttotalAmountHiddenId').val(discountTotal.toFixed(2));
        $('#NetTotalAmountHiddenId').val(netTotal.toFixed(2));
    }

    function dateformate(date) {
        if (date == '' || date == null) {
            return '';
        } else {
            let d = new Date(parseInt(date.substr(6)));
            let _month = parseInt(d.getMonth()) + 1;
            let _date = d.getDate() + "/" + _month + "/" + d.getFullYear();

            return _date;
        }
    }

    function formatDateForDatepicker(date) {
        if (!date) return '';

        let d = new Date(parseInt(date.substr(6)));

        let day = ("0" + d.getDate()).slice(-2);

        let months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun",
            "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

        let month = months[d.getMonth()];

        let year = d.getFullYear();

        return day + "/" + month + "/" + year;
    }

    function formatDateForPicker(date) {

        if (!(date instanceof Date)) {
            date = new Date(date);
        }

        var months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

        var day = date.getDate();
        var month = months[date.getMonth()];
        var year = date.getFullYear();

        return day + "/" + month + "/" + year;
    }

    function parseDate(dateStr) {
        if (dateStr.indexOf("/Date(") === 0) {
            var milliseconds = parseInt(dateStr.match(/\d+/)[0]);
            return new Date(milliseconds);
        }

        if (/^\d{2}\/\d{2}\/\d{4}$/.test(dateStr)) {
            var parts = dateStr.split("/");
            var day = parseInt(parts[0], 10);
            var month = parseInt(parts[1], 10) - 1;
            var year = parseInt(parts[2], 10);
            return new Date(year, month, day);
        }

        if (/^\d{2}\/[A-Za-z]{3}\/\d{4}$/.test(dateStr)) {
            return new Date(dateStr);
        }

        return null;
    }

    function getDaysBetween(from, to) {
        if (!from || !to) return 0;

        var fromDate = parseDate(from);
        var toDate = parseDate(to);

        if (!fromDate || !toDate) return 0;

        const oneDay = 24 * 60 * 60 * 1000;

        fromDate.setHours(0, 0, 0, 0);
        toDate.setHours(0, 0, 0, 0);

        return Math.floor((toDate - fromDate) / oneDay) + 1;
    }

    function parseDateString(dateStr) {
        if (!dateStr) return null;
        var parts = dateStr.split('/');
        if (parts.length === 3) {
            if (isNaN(parts[1])) {
                return new Date(parts[2], getMonthNumber(parts[1]) - 1, parts[0]);
            } else {
                return new Date(parts[2], parseInt(parts[1]) - 1, parts[0]);
            }
        }
        return null;
    }

    function getMonthNumber(monthAbbr) {
        var months = {
            'Jan': 1, 'Feb': 2, 'Mar': 3, 'Apr': 4, 'May': 5, 'Jun': 6,
            'Jul': 7, 'Aug': 8, 'Sep': 9, 'Oct': 10, 'Nov': 11, 'Dec': 12
        };
        return months[monthAbbr] || 1;
    }

    function areDatesEqual(date1, date2) {
        if (!date1 || !date2) return false;

        if (typeof date1 === "string") {
            date1 = parseDateString(date1);
        }

        if (typeof date2 === "string") {
            date2 = parseDateString(date2);
        }

        if (!date1 || !date2) return false;

        return date1.getDate() === date2.getDate() &&
            date1.getMonth() === date2.getMonth() &&
            date1.getFullYear() === date2.getFullYear();
    }

    function initializeReverseDatepickers() {
        $('.from-date-reverse').each(function () {
            var $this = $(this);
            var originalFrom = $this.data('original-from');
            var originalTo = $this.data('original-to');

            $this.datepicker({
                weekStart: 1,
                startDate: originalFrom,
                endDate: originalTo,
                format: "dd/M/yyyy",
                orientation: "bottom left",
                autoclose: true
            }).on('changeDate', function () {
                var $row = $(this).closest('tr');
                var invoiceDetailId = $row.data('id');

                getServiceTypeId(invoiceDetailId, $row);
            });
        });

        $('.to-date-reverse').each(function () {
            var $this = $(this);
            var originalFrom = $this.data('original-from');
            var originalTo = $this.data('original-to');

            $this.datepicker({
                weekStart: 1,
                startDate: originalFrom,
                endDate: originalTo,
                format: "dd/M/yyyy",
                orientation: "bottom left",
                autoclose: true
            }).on('changeDate', function () {
                var $row = $(this).closest('tr');
                var invoiceDetailId = $row.data('id');

                getServiceTypeId(invoiceDetailId, $row);
            });
        });
    }

    function initializeReverseDateSyncHandlers() {
        $('.from-date-reverse, .to-date-reverse')
            .off('changeDate.reverseSync')
            .on('changeDate.reverseSync', function (e, isTriggered) {
                if (isTriggered) return;

                var currentRow = $(this).closest('tr');
                var serviceName = currentRow.data('service-name');

                // Only proceed if rental row
                if (serviceName && serviceName.includes('rental')) {

                    var fromDate = currentRow.find('.from-date-reverse').val();
                    var toDate = currentRow.find('.to-date-reverse').val();

                    var nextRow = currentRow.next('tr');

                    // Check if next row is cleaning
                    if (nextRow.length && nextRow.data('service-name').includes('cleaning')) {

                        nextRow.find('.from-date-reverse')
                            .val(fromDate)
                            .trigger('change', [true])
                            .prop('readonly', true)
                            .prop('disabled', true)
                            .addClass('disabled-date-field');

                        nextRow.find('.to-date-reverse')
                            .val(toDate)
                            .trigger('change', [true])
                            .prop('readonly', true)
                            .prop('disabled', true)
                            .addClass('disabled-date-field');
                    }
                }
            });

        makeCleaningDatesReadonly();
    }

    function recalculateCleaningAmount(cleaningRow, fromDate, toDate) {
        var days = getDaysBetween(fromDate, toDate);
        if (!days || days == 0) return;

        var basePrice = parseFloat(cleaningRow.data('base-service-price'));
        var frequencyId = $('#FrequencyId').val();
        if (!frequencyId) return;

        var calculatedAmount = basePrice;

        // DAILY
        if (frequencyId == 1) {
            calculatedAmount = basePrice * days;
        }

        // MONTHLY (PRORATED)
        if (frequencyId == 2) {
            calculatedAmount = 0;

            var startDate = new Date(fromDate);
            var endDate = new Date(toDate);
            var currentMonthStart = new Date(startDate);
            currentMonthStart.setDate(1);

            while (currentMonthStart <= endDate) {
                var daysInMonth = new Date(
                    currentMonthStart.getFullYear(),
                    currentMonthStart.getMonth() + 1,
                    0
                ).getDate();

                var monthStart = currentMonthStart < startDate ? startDate : currentMonthStart;

                var monthEnd = new Date(
                    currentMonthStart.getFullYear(),
                    currentMonthStart.getMonth() + 1,
                    0
                );

                if (monthEnd > endDate) {
                    monthEnd = endDate;
                }

                var daysInThisMonth = GetDatesDiff(monthStart, monthEnd);
                var proratedAmount = (basePrice / daysInMonth) * daysInThisMonth;

                calculatedAmount += proratedAmount;

                currentMonthStart.setMonth(currentMonthStart.getMonth() + 1);
                currentMonthStart.setDate(1);
            }
        }

        // WEEKLY
        if (frequencyId == 3) {
            calculatedAmount = (basePrice / 7) * days;
        }

        cleaningRow.find('.calculated-amount').val(calculatedAmount.toFixed(2));

        var discountPercentage = parseFloat(cleaningRow.find('.discount-percentage').val()) || 0;
        var taxId = cleaningRow.find('.taxes').val();

        var discountAmount = 0, taxAmount = 0;
        if (discountPercentage > 0) {
            discountAmount = Math.round((calculatedAmount * (discountPercentage / 100)) * 100) / 100;
        }

        var adjustedNetAmount = calculatedAmount - discountAmount;

        if (taxId) {
            var taxPercentage = GetTaxPercentage(taxId);
            taxAmount = Math.round((adjustedNetAmount * (taxPercentage / 100)) * 100) / 100;
        }

        cleaningRow.find(".discount-percentage").data("discount-amount", discountAmount);
        cleaningRow.find(".taxes").data("tax-amount", taxAmount);

        var netAmount = calculatedAmount + taxAmount - discountAmount;
        cleaningRow.find('.net-total-amount').val(netAmount.toFixed(2));

        calculateReverseTotals();
    }

    function makeCleaningDatesReadonly() {
        $('#reverseInvoiceTableBody tr').each(function () {
            var serviceName = $(this).data('service-name');

            if (serviceName && serviceName.includes('rental charges')) {

                $(this).find('.to-date-reverse')
                    .prop('readonly', true)
                    .prop('disabled', true)
                    .addClass('disabled-date-field');
            }

            if (serviceName && serviceName.includes('cleaning')) {

                $(this).find('.from-date-reverse, .to-date-reverse')
                    .prop('readonly', true)
                    .prop('disabled', true)
                    .addClass('disabled-date-field');
            }
        });
    }

    function GetDatesDiff(fromDate, toDate) {
        var days = 0;
        if (fromDate != "" && toDate != "") {
            var frmDt = new Date(fromDate);
            var todt = new Date(toDate);
            todt = new Date(todt.setDate(todt.getDate() + 1));
            var diff = todt - frmDt;
            days = diff / (1000 * 3600 * 24);
        }
        return days;
    }

    function validateRowDates($row) {
        var fromInput = $row.find('.from-date-reverse');
        var toInput = $row.find('.to-date-reverse');
        var calculatedAmount = $row.find('.calculated-amount');

        var originalFrom = fromInput.data('original-from');
        var originalTo = fromInput.data('original-to');
        var originalPrice = $row.data('original-price');
        var basePrice = $row.data('base-service-price');
        var originalDays = $row.data('original-days');
        var discountPercentage = parseFloat($row.find('.discount-percentage').val()) || 0;
        var taxId = $row.find('.taxes').val();

        var currentFrom = fromInput.val();
        var currentTo = toInput.val();

        if (!currentFrom || !currentTo) return;

        var isFromDateMatch = areDatesEqual(currentFrom, originalFrom);
        var isToDateMatch = areDatesEqual(currentTo, originalTo);

        var isValid = (isFromDateMatch || isToDateMatch);

        if (!isValid) {
            $row.addClass('danger');
            showValidationMessage("Each row must have at least one date matching the original and a valid date range.");
        } else {
            $row.removeClass('danger');
            hideValidationMessage();
            var days = GetDatesDiff(currentFrom, currentTo);
            var frequencyId = $('#FrequencyId').val();
            var newAmount = basePrice;

            if (days != 0 && frequencyId == 1) {
                newAmount = basePrice * days;
            }

            if (days != 0 && frequencyId == 2) {
                // Monthly (Prorated)
                newAmount = 0;

                var startDate = new Date(currentFrom);
                var endDate = new Date(currentTo);
                var currentMonthStart = new Date(startDate);
                currentMonthStart.setDate(1);

                while (currentMonthStart <= endDate) {
                    var daysInMonth = new Date(
                        currentMonthStart.getFullYear(),
                        currentMonthStart.getMonth() + 1,
                        0
                    ).getDate();

                    var monthStart = currentMonthStart < startDate ? startDate : currentMonthStart;

                    var monthEnd = new Date(
                        currentMonthStart.getFullYear(),
                        currentMonthStart.getMonth() + 1,
                        0
                    );

                    if (monthEnd > endDate) {
                        monthEnd = endDate;
                    }

                    var daysInThisMonth = GetDatesDiff(monthStart, monthEnd);
                    var proratedAmount = (basePrice / daysInMonth) * daysInThisMonth;
                    newAmount += proratedAmount;

                    currentMonthStart.setMonth(currentMonthStart.getMonth() + 1);
                    currentMonthStart.setDate(1);
                }
            }

            if (days != 0 && frequencyId == 3) {
                // Weekly
                newAmount = (basePrice / 7) * days;
            }

            if (!isNaN(newAmount)) {
                calculatedAmount.val(newAmount.toFixed(2));
            }

            var discountAmount = 0;
            var taxAmount = 0;

            if (discountPercentage > 0) {
                discountAmount = Math.round((newAmount * (discountPercentage / 100)) * 100) / 100;
            }

            var adjustedNetAmount = newAmount - discountAmount;
            if (taxId) {
                var taxPercentage = GetTaxPercentage(taxId);
                taxAmount = Math.round((adjustedNetAmount * (taxPercentage / 100)) * 100) / 100;
            }

            $row.find(".discount-percentage").data("discount-amount", discountAmount);
            $row.find(".taxes").data("tax-amount", taxAmount);

            updateRowNetAmount($row);
        }

        var allValid = true;
        $('#reverseInvoiceTableBody tr').each(function () {
            if ($(this).hasClass('danger')) {
                allValid = false;
            }
        });

        updateReverseSubmitButtonState();
    }

    function showValidationMessage(message) {
        $('.reverse-validation-message').remove();

        var msgHtml = `<div class="alert alert-warning reverse-validation-message" style="margin-top: 10px;">
                    <i class="fa fa-exclamation-triangle"></i> ${message}
               </div>`;
        $('#reverseInvoiceTable').after(msgHtml);
    }

    function hideValidationMessage() {
        $('.reverse-validation-message').remove();
    }

    function updateRowNetAmount($row) {
        var amount = parseFloat($row.find(".calculated-amount").val()) || 0;
        var taxAmount = parseFloat($row.find(".taxes").data("tax-amount")) || 0;
        var discountAmount = parseFloat($row.find(".discount-percentage").data("discount-amount")) || 0;
        var netAmount = amount + taxAmount - discountAmount;
        $row.find(".net-total-amount").val(netAmount.toFixed(2));
    }

    function submitReverseInvoice(locationId) {
        if (reverseCalcPendingCount > 0) {
            return;
        }

        var remarks = $.trim($('#reverseInvoiceRemarks').val());
        if (!remarks) {
            showMessage('Please enter remarks.');
            return;
        }

        var submitBtn = $('#ReverseInvoiceBtnConfirm .btn-ok');
        submitBtn.prop('disabled', true);

        ShowLoader();
        var invoiceDetails = [];

        $('#reverseInvoiceTableBody tr').each(function (index) {
            var $row = $(this);

            var detail = {
                Id: parseInt($row.data('id')),
                InvoicingId: $row.data('invoicing-id'),
                ServiceId: $row.data('service-id'),
                ServiceName: $row.find('td:eq(0)').text().trim(),
                Description: $row.find('td:eq(1)').text().trim(),
                FromDate: $row.find('.from-date-reverse').val(),
                ToDate: $row.find('.to-date-reverse').val(),
                Price: parseFloat($row.find('.calculated-amount').val()) || 0,
                TaxesIds: ($row.find('.taxes').val() || []).join(','),
                DiscountPercentage: parseFloat($row.find('.discount-percentage').val()) || 0,
                TotalAmount: parseFloat($row.find('.net-total-amount').val()) || 0,
            };
            invoiceDetails.push(detail);
        });

        var reverseData = {
            Id: parseInt($('#ReveserInvoiceHiddenId').val()),
            StudentId: parseInt($("#StudentId").val()),
            LocationId: locationId,
            InvoiceTypeId: parseInt($('#InvoiceTypeId').val()) || 0,
            Remarks: remarks,
            SubTotal: parseFloat($('#SubtotalAmountHiddenId').val()) || 0,
            TaxAmount: parseFloat($('#TaxtotalAmountHiddenId').val()) || 0,
            DiscountAmount: parseFloat($('#DiscounttotalAmountHiddenId').val()) || 0,
            NetTotalAmount: parseFloat($('#NetTotalAmountHiddenId').val()) || 0,
            InvoiceDetails: invoiceDetails
        };

        $.ajax({
            type: "POST",
            url: GetFormatedUrl('/Invoicings/ReverseInvoice'),
            data: JSON.stringify(reverseData),
            contentType: 'application/json',
            success: function (response) {
                if (response.success) {
                    window.location.replace(GetFormatedUrl('/Invoicings/Index'));
                    showMessage(response.message);
                } else {
                    $('#ReverseInvoiceBtnConfirm').modal('show');
                    showMessage(response.message || "Something went wrong.");
                    submitBtn.prop('disabled', false);
                }
                HideLoader();
            },
            beforeSend: function () {
                ShowLoader();
                $('#ReverseInvoiceBtnConfirm').modal('hide');
            },
            error: function (xhr, status, error) {
                console.error('Ajax error:', error, thrown);
                $('#ReverseInvoiceBtnConfirm').modal('show');
                showMessage('Ajax error:', error, thrown);
                submitBtn.prop('disabled', false);
                HideLoader();
            },
            complete: function () {
                HideLoader();
            },
        });
    }

    $('#ReverseInvoiceBtnConfirm').on('hidden.bs.modal', function () {
        reverseCalcPendingCount = 0;
        $(this).find('.reverse-calc-overlay').removeClass('active');
        $('#reverseInvoiceRemarks').val('');
        $('#ReverseInvoiceBtnConfirm .btn-ok').prop('disabled', false);
    });

    $('#reverseInvoiceRemarks').on('input', function () {
        updateReverseSubmitButtonState();
    });

    function OpenConfirmReverseInvoiceModalForMuscat(id, locationId, invoiceTypeId) {

        $('#ReveserInvoiceForMuscatHiddenId').val(id);
        $('#ReveserInvoiceForMuscatLocationHiddenId').val(locationId);
        $('#ReveserInvoiceForMuscatInvoiceTypeHiddenId').val(invoiceTypeId);
        $('#reverseInvoiceRemarksForMuscat').val('');

        $('#ReverseInvoiceBtnConfirmForMuscat').modal('show');

        $('#ReverseInvoiceBtnConfirmForMuscat form').off('submit').on('submit', function (event) {
            event.preventDefault();

            var remarks = $.trim($('#reverseInvoiceRemarksForMuscat').val());
            if (!remarks) {
                showMessage('Please enter remarks.');
                return;
            }

            $('#ReverseInvoiceBtnConfirmForMuscat .btn-ok').prop('disabled', true);

            $('.Customoverlay, .Customloader').show();
            $.ajax({
                type: "POST",
                url: $(this).attr('action'),
                data: $(this).serialize(),
                success: function (data) {
                    if (data && data.success === false) {
                        showMessage(data.message || 'Something went wrong.');
                        $('#ReverseInvoiceBtnConfirmForMuscat .btn-ok').prop('disabled', false);
                        return;
                    }
                    window.location.replace(GetFormatedUrl('/Invoicings/Index'));
                },
                error: function () {
                    showMessage('Some error occurred!', 'error');
                    $('#ReverseInvoiceBtnConfirmForMuscat .btn-ok').prop('disabled', false);
                },
                complete: function () {
                    $('.Customoverlay, .Customloader').hide();

                }
            });
        });
    }

    $('#ReverseInvoiceBtnConfirmForMuscat').on('hidden.bs.modal', function () {
        $('#reverseInvoiceRemarksForMuscat').val('');
        $('#ReverseInvoiceBtnConfirmForMuscat .btn-ok').prop('disabled', false);
    });

    var Id = 0;
    function ApproveConform(Code) {
        Id = Code;
        $('#ApproveConformation').modal('show');
    }

    function ConfirmPayment(id, locationId) {
        debugger
        Id = id;
        $.ajax({
            url: GetFormatedUrl('/Invoicings/GetPayableInvoice'),
            type: "Get",
            data: { id: id },
            success: function (response) {
                if (response.success) {
                    var data = response.data;
                    $("#paymentConfirmBody").html('');
                    var resolvedLocationId = parseInt(locationId || data.LocationId || "0");

                    var html = `<p>Invoice Code  <b style="color:#c20e1a">${data.Code}</b></p>
                            <input id="PaymentLink_InvoiceId" value='${data.Id}' hidden/>
                            <input id="PaymentLink_LocationId" value='${resolvedLocationId}' hidden/>
                            <p>Net Amount is  <b style="color:#c20e1a">${data.NetAmount}</b>`;
                    $("#paymentConfirmBody").html(html);
                    $("#ConfirmPaymentLink").modal();
                } else {
                    Toast.show("Invoice not Found!", "error");
                }
            }
        });

    }

    function ApproveInvoice() {
        $('#ApproveConformation').modal('hide');
        $.ajax({
            type: "Get",
            url: GetFormatedUrl('/Invoicings/ApproveInvoice/') + Id,
            async: true,
            success: function (data) {
                var data = data.status;
                if (data == true) {
                    $('.ToBeApproved-' + Id).closest('tr').find('.EditHideAfterApproval').addClass('hide');
                    $('.ToBeApproved-' + Id).closest('tr').find('.ApproveHideAfterApproval').addClass('hide');
                    $('.ToBeApproved-' + Id).closest('td').html('<span class="text-success label label-success">Approved</span>');
                }
            },
            error: function (data) {
            }
        });
    };

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

    function GenratePaymentLink() {
        debugger
        $("#ConfirmPaymentLink").modal('hide');
        var Id = $("#PaymentLink_InvoiceId").val();
        var locationId = parseInt($("#PaymentLink_LocationId").val() || "0");
        var muscatLocationId = cfg.muscatLocationId;
        var dubaiLocationId = cfg.dubaiLocationId;
        var paymentLinkEndpoint = '/Invoicings/GetPaymentLink/';

        if (locationId === dubaiLocationId) {
            paymentLinkEndpoint = '/Invoicings/CreatePaymentLink/';
        } else if (locationId !== muscatLocationId) {
            Toast.show("Payment link is not configured for this location.", "error");
            return;
        }

        $.ajax({
            type: "Get",
            url: GetFormatedUrl(paymentLinkEndpoint) + Id,
            async: true,
            success: function (data) {
                var data = data;
                if (data.success) {
                    $("#paymentlink").val(data.data);
                    $("#PaymentLinkModal").modal();
                } else {
                    window.location.href = "/Invoicings/Index";
                }
            },
            error: function (data) {
            }
        });
    }

    function Copy() {
        var copyText = document.getElementById("paymentlink");
        copyText.select();
        copyText.setSelectionRange(0, 99999);

        navigator.clipboard.writeText(copyText.value);
    }

    function GetExcel() {
        var SD = $('.from_date').val();
        var ED = $('.to_date').val();
        var InvoiceTypeId = $('#InvoiceTypeId').val();

        window.location.href = GetFormatedUrl("/Invoicings/ExportInvoiceReport?FromDate=" + SD + "&ToDate=" + ED + "&InvoiceTypeId=" + InvoiceTypeId);

    }

    // Expose functions used by inline onclick handlers and other view code.
    window.GetInvoices = GetInvoices;
    window.GetExcel = GetExcel;
    window.ApproveConform = ApproveConform;
    window.ApproveInvoice = ApproveInvoice;
    window.ConfirmPayment = ConfirmPayment;
    window.GenratePaymentLink = GenratePaymentLink;
    window.Copy = Copy;
    window.OpenConfirmReverseInvoiceModal = OpenConfirmReverseInvoiceModal;
    window.OpenConfirmReverseInvoiceModalForMuscat = OpenConfirmReverseInvoiceModalForMuscat;
    window.GetTaxes = GetTaxes;
    window.GetTaxPercentage = GetTaxPercentage;
})();
