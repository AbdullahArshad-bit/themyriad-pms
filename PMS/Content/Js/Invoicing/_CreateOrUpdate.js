$(document).ready(function () {

    bindServiceDates();
    GetFrequency();
    var invoiceTypeId = $('#InvoiceTypeId').val();
    var studentId = $('.StudentId').val();
    if (!invoiceTypeId || !studentId) {
        $('select.items').prop('disabled', true);
    }
});

var options = {

    url: function (phrase) {
        return '/pms/invoicings/GetServicesList';
    },
    listLocation: "data",
    getValue: function (data) {
        var name = data.name;
        var id = data.code;
        return id + ': ' + name;
    },

    list: {
        match: {
            enabled: true
        },

    },

    theme: "square"
};

$(function () {

    if ($("#InvoiceTypeId").val() != 1) {
        $(".datetd").hide();
    }
    $('body').on('change', '.taxes', function () {
        var Taxes = $(this).val();
        var taxpercentages = [];
        $(Taxes).each(function (index, element) {
            var val = GetTaxPercentage(element);
            taxpercentages.push(val);
        })
        var percentageincomma = taxpercentages.join(',');
        var Amount = $(this).closest('tr').find('.netamountCls').val();
        var netamount = Amount;
        if (taxpercentages.length == 0)
            netamount = Number(Amount).toFixed(2);
        $(taxpercentages).each(function (i, e) {
            tax = Math.round((Amount / 100 * e) * 100) / 100;
            netamount = Number(netamount) + Number(tax);
        })
        $(this).closest('tr').find('.netamount').val(Number(netamount).toFixed(2));

        CalculateAdjustmentAmount();
    })

    $('body').on('keyup', '.netamountCls', function () {

        calculatenetAmount(this);
        CalculateAdjustmentAmount();
    })
    $('body').on('change', '.StudentId', function () {
        ResetDetail();
    });
})


$("#table1").on('click', '.btnDelete', function () {
    $(this).closest('tr').remove();
    CalculateAdjustmentAmount();

});

$("#table1").on('keydown', '.netamountCls', function (e) {
    var keyCode = e.keyCode || e.which;

    if (keyCode == 9) {
        addInvoiceRow();
    }
});

$("#table1 input[name*='[code]']").autocomplete({
    lookup: function (query, done) {
        var result = [];

        var invoicetypeid = $('#InvoiceTypeId').val();
        if (invoicetypeid == null || invoicetypeid == undefined || invoicetypeid == '') {
            showMessage('Please Select Invoice Type first!');
            return;
        }
        $.ajax({
            type: "Get",
            async: false,
            url: GetFormatedUrl('/invoicings/GetServicesList'),
            data: {
                InvoicetypeId: invoicetypeid
            },
            success: function (data) {
                result = data
                return result;
            }
        });

        done(result);
    },
    onSelect: function (suggestion) {

        $(this).parent().parent().find('.items').attr('data-id', suggestion.data);
        $(this).parent().parent().find('.items').removeClass('highlight');
        $(this).parent().parent().find('.Desc').focus();

        GetResidentOccupancy(suggestion, this);
        CalculateAdjustmentAmount();


    }
});

$('body').on('change', '.items', function () {
    var selected = $(this).find('option:selected');
    var suggestion = {
        data: selected.attr('data-id'),
        value: selected.val(),
        price: selected.attr('data-price'),
        taxId: selected.attr('data-taxid'),
        servicetypeid: selected.attr('data-servicetypeid')
    };
    $(this).attr('data-id', suggestion.data);
    $(this).removeClass('highlight');

    if (!suggestion.data) {
        var row = $(this).closest('tr');
        row.find('.netamountCls').val('').prop('disabled', true).prop('readonly', true).show();
        row.find('.basenetamountCls').val('').prop('disabled', true).prop('readonly', true).show();
        row.find('.Desc').val('').prop('disabled', true).prop('readonly', true).show();
        row.find('.taxes').val('').change();
        row.find('#DiscountPercentage').val('').change();
        row.find('.deposit-price-select').remove();
        row.find('#fromDate, #toDate').val('').prop('readonly', false).prop('disabled', false).removeClass('disabled-date-field');
        $(this).attr('data-id', '');
        return;
    };
    var invoiceTypeId = $('#InvoiceTypeId').val();
    var studentId = $('.StudentId').val();
    var row = $(this).closest('tr');

    // For deposit invoices, check if there are multiple bookings
    if (invoiceTypeId == 2) { // 2 = Deposit
        ShowLoader();
        $.ajax({
            type: 'GET',
            url: GetFormatedUrl('/invoicings/GetDepositOptions'),
            data: { serviceId: suggestion.data, studentId: studentId },
            success: function (response) {
                HideLoader();
                if (!response.success) {
                    showMessage(response.message);
                    return;
                }
                if (response.multiple) {
                    // Remove any existing custom dropdown
                    row.find('.deposit-price-select').remove();
                    row.find('.deposit-price-display').remove();
                    var amountCell = row.find('.netamountCls');
                    amountCell.hide();

                    // Build custom dropdown
                    var wrapper = $('<div class="deposit-price-select" style="position:relative; display:inline-block; width:100%;"></div>');

                    var displayBox = $('<div class="deposit-display-box form-control" style="cursor:pointer; background:#fff; display:flex; justify-content:space-between; align-items:center;">' +
                        '<span class="deposit-selected-text" style="color:#999;">--- Select Term Price ---</span>' +
                        '<span>&#9660;</span>' +
                        '</div>');

                    var optionsList = $('<ul class="deposit-options-list" style="display:none; position:absolute; z-index:9999; background:#fff; border:1px solid #ccc; width:100%; list-style:none; padding:0; margin:0; max-height:200px; overflow-y:auto;"></ul>');

                    $.each(response.data, function (i, opt) {
                        var li = $('<li style="padding:8px 12px; cursor:pointer; border-bottom:1px solid #f0f0f0;">' + opt.label + '</li>');
                        li.data('price', opt.price);
                        li.data('label', opt.label);
                        li.data('occupancy', opt.occupancy);
                        li.data('taxid', opt.taxId);
                        li.data('occupancyid', opt.occupancyId);

                        li.on('mouseenter', function () { $(this).css('background', '#f5f5f5'); });
                        li.on('mouseleave', function () { $(this).css('background', '#fff'); });

                        li.on('click', function () {
                            var price = $(this).data('price');

                            // Show only price in the display box
                            displayBox.find('.deposit-selected-text').text(price).css('color', '#333');
                            optionsList.hide();

                            // Sync all values
                            amountCell.val(price);
                            row.find('.netamountCls').val(price).prop('disabled', false).prop('readonly', false);
                            row.find('.basenetamountCls').val(price).prop('disabled', false);
                            row.find('.Desc').val($(this).data('occupancy')).prop('disabled', true);
                            row.find('.taxes').val($(this).data('taxid')).change();
                            $('#OccupancyId').val($(this).data('occupancyid'));
                            $('#ConfigId').val($(this).data('occupancyid'));

                            GetFrequency();
                            CalculateAdjustmentAmount();
                        });

                        optionsList.append(li);
                    });

                    // Toggle open/close on display box click
                    displayBox.on('click', function () {
                        optionsList.toggle();
                    });

                    // Close when clicking outside
                    $(document).on('click.depositDropdown', function (e) {
                        if (!wrapper.is(e.target) && wrapper.has(e.target).length === 0) {
                            optionsList.hide();
                        }
                    });

                    wrapper.append(displayBox).append(optionsList);
                    amountCell.after(wrapper);
                }

                else {
                    // Only one booking — proceed as normal
                    GetResidentOccupancy(suggestion, row.find('.items')[0]);
                    CalculateAdjustmentAmount();
                }
            },
            error: function () {
                HideLoader();
                GetResidentOccupancy(suggestion, row.find('.items')[0]);
                CalculateAdjustmentAmount();
            }
        });
    } else {
        GetResidentOccupancy(suggestion, this);
        CalculateAdjustmentAmount();
    }
});
$('body').on('blur', '.netamountCls', function (ev) {
    CalculateAdjustmentAmount();
});


$("#InvoiceTypeId").on('change', function (e) {

    var typeId = $(this).val();
    ResetDetail();
    if (typeId == 1)
        $('.datetd').show();
    else
        $('.datetd').hide();

    $('.date').val('');
    var studentId = $('#StudentId').val();
    if (!studentId) {
        $('select.items').prop('disabled', true);
    }
});

var table = $('#table1');
var counter = table.childElementCount == undefined ? 0 : table.childElementCount;
var trigger = true;

$('.Addnewrow').on('click', function () {
    addInvoiceRow();
})
function addInvoiceRow(autoTrigger = false) {
    var LocationIdForNetAmount = $('#LocationId').val();

    var InvoiceTypeId = $("#InvoiceTypeId").val();
    var markup = "<tr class='parentRowGrider'> ";
    markup = markup + " <td>";
    markup = markup + " <select name='det[" + counter + "][code]' class='items form-control' disabled>";
    markup = markup + " <option value=''>--- Select Service ---</option>";
    markup = markup + " </select>";
    markup = markup + " </td>";
    markup = markup + " <td>";
    markup = markup + " <input name=det[" + counter + "][qnt] type='text' class='form-control reqvalue NotNegative LimitChecker Desc' placeholder='Description' value='' />";
    markup = markup + " </td>";
    if (InvoiceTypeId == '1') {
        markup = markup + " <td>";
        markup = markup + " <input name=det[" + counter + "][fromDate] type='text'  id='fromDate' class='form-control date' onfocus='Dateset()' placeholder='DD/MM/YYYY' autocomplete='off'/>";
        markup = markup + " </td>";
        markup = markup + " <td>";
        markup = markup + " <input name=det[" + counter + "][toDate] type='text'  id='toDate' class='form-control date' onfocus='Dateset()' placeholder='DD/MM/YYYY' autocomplete='off'/>";
        markup = markup + " </td>";
    }
    markup = markup + " <td style='display:none;'>";
    markup = markup + " <input name=det[" + counter + "][baselastbuyrate] type='text' class='form-control  basenetamountCls decimals' value='' readonly='readonly'/>";
    markup = markup + " </td>";
    markup = markup + " <td>";

    if (parseInt(LocationIdForNetAmount) === dubaiLocationId && parseInt(InvoiceTypeId) === miscellaneousInvoiceTypeId && editInvoiceAmountRole == true) {
        markup = markup + " <input name=det[" + counter + "][lastbuyrate] type='text' class='form-control  netamountCls decimals' placeholder='Amount' value=''/>";
    } else {
        markup = markup + " <input name=det[" + counter + "][lastbuyrate] type='text' class='form-control  netamountCls decimals' placeholder='Amount' value='' readonly />";
    }
    markup = markup + " </td>";
    markup = markup + " <td>";
    markup = markup + '<select name=det[' + counter + '][TaxesIds] autocomplete="off" class="form-control select2" id="TaxIds" multiple="multiple" name="TaxIds"></select>'
    markup = markup + " </td>";
    if (userHasDiscountRole == true) {

        markup += "<td>";
        markup += "<input name=det[" + counter + "][DiscountPercentage] type='text' class='form-control discountPercentageCls' id='DiscountPercentage' placeholder='Discount' />";
        markup += "</td>";
    }
    else {
        markup += "<td>";
        markup += "<input name=det[" + counter + "][DiscountPercentage] type='text' class='form-control discountPercentageCls' id='DiscountPercentage' placeholder='Discount' readonly/>";
        markup += "</td>";
    }
    markup = markup + " <td>";
    markup = markup + "<input name=det[" + counter + "][NetAmountPlusTax] type='text'' class='form-control  netamount' placeholder='Net Amount' readonly>"
    markup = markup + " </td>";
    markup = markup + " <td><a href='#D' onclick='deleterow(" + counter + ")' class='delete btn btn-danger btn-sm btn-bordered waves-effect w-md  btnDelete'>Delete</a></td>";
    markup = markup + " </tr>";
    table.append(markup);
    var newRow = table.find("tr.parentRowGrider").last();
    var newSelect = newRow.find("select.items");
    populateServiceDropdown(newSelect);
    GetTaxes('det[' + counter + '][TaxesIds]');
    $("[name='det[" + counter + "][TaxesIds]']").addClass('taxes');
    $('.select2').select2();

    counter = counter + 1;
    if (autoTrigger === true) {
        autoTriggerSelectForLastRowWithAjax(newSelect);
    }
    bindServiceDates();

}
// Modify the autoTriggerSelectForLastRowWithAjax function to add event handlers after selection
function autoTriggerSelectForLastRowWithAjax(inputElement) {
    const invoiceTypeId = $('#InvoiceTypeId').val();
    const row = $(inputElement).closest('tr');

    $.ajax({
        type: "GET",
        async: false,
        url: GetFormatedUrl('/invoicings/GetServicesList'),
        data: { InvoicetypeId: invoiceTypeId },
        success: function (data) {
            const match = data.suggestions.find(x => x.value.toLowerCase().includes('cleaning'));
            if (!match) {
                HideLoader();
                return;
            }

            // Build and populate the dropdown options first
            var options = '<option value="">--- Select Service ---</option>';
            $.each(data.suggestions, function (i, svc) {
                options += '<option value="' + svc.value + '"'
                    + ' data-id="' + svc.data + '"'
                    + ' data-price="' + svc.price + '"'
                    + ' data-taxid="' + svc.taxId + '"'
                    + ' data-servicetypeid="' + svc.servicetypeid + '">'
                    + svc.value + '</option>';
            });
            row.find('select.items').html(options);

            // Now select the cleaning option
            row.find('select.items').val(match.value);
            row.find('select.items').attr('data-id', match.data);

            // Copy dates from the previous rental row
            const prevRow = row.prev('tr');
            if (prevRow.length && prevRow.find('select.items').val().toLowerCase().includes('rental')) {
                const fromDate = prevRow.find('#fromDate').val();
                const toDate = prevRow.find('#toDate').val();
                if (fromDate) row.find('#fromDate').val(fromDate);
                if (toDate) row.find('#toDate').val(toDate);
            }
            row.find('select.items').val(match.value).prop('disabled', true).prop('readonly', true);
            row.find('select.items').attr('data-id', match.data).prop('disabled', true).prop('readonly', true);

            GetResidentOccupancy(match, inputElement);
            CalculateAdjustmentAmount();
        },
        error: function () {
            HideLoader();
        }
    });
}

function populateServiceDropdown(selectElement) {
    var invoiceTypeId = $('#InvoiceTypeId').val();
    var studentId = $('.StudentId').val();

    if (!invoiceTypeId || !studentId) {
        selectElement.prop('disabled', true).html('<option value="">--- Select Service ---</option>');
        return;
    }

    $.ajax({
        type: "GET",
        async: false,
        url: GetFormatedUrl('/invoicings/GetServicesList'),
        data: { InvoicetypeId: invoiceTypeId },
        success: function (data) {
            var options = '<option value="">--- Select Service ---</option>';
            $.each(data.suggestions, function (i, svc) {
                options += '<option value="' + svc.value + '" data-id="' + svc.data + '" data-price="' + svc.price + '" data-taxid="' + svc.taxId + '" data-servicetypeid="' + svc.servicetypeid + '">' + svc.value + '</option>';
            });
            selectElement.html(options).prop('disabled', false);
        }
    });
}

function bindServiceAutocomplete(inputElement) {
    inputElement.autocomplete({
        lookup: function (query, done) {
            let result = [];
            const invoicetypeid = $('#InvoiceTypeId').val();
            $.ajax({
                type: "GET",
                async: false,
                url: GetFormatedUrl('/invoicings/GetServicesList'),
                data: { InvoicetypeId: invoicetypeid },
                success: function (data) {
                    result = data;
                }
            });
            done(result);
        },
        onSelect: function (suggestion) {
            trigger = true;
            const row = $(this).closest('tr');
            row.find('.items').attr('data-id', suggestion.data).removeClass('highlight');
            row.find('.Desc').focus();

            GetResidentOccupancy(suggestion, this);
            CalculateAdjustmentAmount();
        }
    });
}

// Modify GetResidentOccupancy function to add date sync after adding a new cleaning fee row
function GetResidentOccupancy(suggestion, element) {
    ShowLoader();

    const LocationIdForNetAmount = $('#LocationId').val();
    const studentId = $("#StudentId").val();
    const serviceId = suggestion.data;
    const serviceTypeId = suggestion.servicetypeid;
    const invoiceTypeId = $("#InvoiceTypeId").val();
    const taxId = suggestion.taxId;
    const row = $(element).closest('tr');

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
            if (!response.status) {
                showMessage("No package available against this student");
                resetRowFields(row, LocationIdForNetAmount);
                HideLoader(); // Ensure loader is hidden even on failure
                return;
            }

            const res = response.data;
            fillRowFields(row, res, LocationIdForNetAmount, taxId);

            if (isRentalInvoice) {
                CalculateRentalServiceAmount(element);

                if (LocationIdForNetAmount == "17" &&
                    suggestion.value.toLowerCase().includes('rental') &&
                    trigger
                ) {
                    addInvoiceRow(true);
                    const lastRow = $('.parentRowGrider').last();
                    autoTriggerSelectForLastRowWithAjax(lastRow.find('.items'));
                }
            }

            initializeDateSyncHandlers();
            HideLoader();
        },
        error: function (xhr, status, error) {
            showMessage("An error occurred while fetching occupancy data.");
            HideLoader();
        }
    });

}
function resetRowFields(row, locationId) {
    row.find('.basenetamountCls').val("").prop('disabled', true);
    row.find('.netamountCls').val("").prop('disabled', locationId != "17" || !editInvoiceAmountRole).keyup();
    row.find('.Desc').val("").prop('disabled', true);
}

function fillRowFields(row, res, locationId, taxId) {
    row.find('.basenetamountCls').val(res.ServicePrice).prop('disabled', true);
    row.find('.netamountCls').val(res.ServicePrice).prop('disabled', locationId != "17").keyup();
    row.find('.Desc').val(res.Occupancy).prop('disabled', true);
    row.find('.taxes').val(taxId).change();
    $('#OccupancyId').val(res.OccupancyId);
    $('#ConfigId').val(res.OccupancyId);
    GetFrequency();
}
$(document).on('input', '.discountPercentageCls', function () {
    var currentRow = $(this).closest('tr');
    var currentServiceText = currentRow.find('.items').val().toLowerCase();

    if (currentServiceText.includes('rental')) {
        var discountPercentage = $(this).val();
        var nextRow = currentRow.next('tr');

        if (nextRow.length && nextRow.find('.items').val().toLowerCase().includes('cleaning')) {
            nextRow.find('.discountPercentageCls').val(discountPercentage);

            nextRow.find('.discountPercentageCls')
                .prop('readonly', true)
                .prop('disabled', true);

            //CalculateAdjustmentAmount();
        }
    }

    CalculateAdjustmentAmount();
});

var CalculateAdjustmentAmount = function () {

    var sum = 0;
    var sumofNetAmount = 0;
    var sumofTaxAmount = 0;
    var sumofDiscountAmount = 0; // New variable to store the total discount amount

    $(".parentRowGrider").each(function () {
        var netAmount = parseFloat($(this).find('.netamountCls').val());
        var discountPercentage = parseFloat($(this).find('.discountPercentageCls').val());

        // Calculate adjusted net amount based on discount
        var adjustedNetAmount = netAmount;
        var discountAmount = 0; // Variable to store the discount amount for the current row

        if (!isNaN(netAmount) && !isNaN(discountPercentage)) {
            discountAmount = Math.round((netAmount * (discountPercentage / 100)) * 100) / 100; // Calculate the discount amount
            adjustedNetAmount -= discountAmount;
        }

        // Calculate tax amount based on adjusted net amount
        var taxAmount = 0;
        var taxId = $(this).find('.taxes').val(); // Get the selected tax ID directly
        if (!taxId) {
            return; // Skip processing if no tax ID is selected
        }

        var taxPercentage = GetTaxPercentage(taxId);
        var tax = Math.round((adjustedNetAmount * (taxPercentage / 100)) * 100) / 100;
        taxAmount += tax;

        sum += adjustedNetAmount + taxAmount;
        sumofNetAmount += netAmount; // Sum of actual net amounts (without discounts or taxes)
        sumofDiscountAmount += discountAmount; // Add the discount amount to the total
        sumofTaxAmount += taxAmount;

        $(this).find('.netamount').val((adjustedNetAmount + taxAmount).toFixed(2));
    });

    var SubTotal = sumofNetAmount.toFixed(2); // SubTotal as the sum of actual net amounts
    var TaxAmount = sumofTaxAmount.toFixed(2);
    var TotalDiscountAmount = sumofDiscountAmount.toFixed(2);
    var Total = (parseFloat(SubTotal) + parseFloat(TaxAmount) - parseFloat(TotalDiscountAmount)).toFixed(2);

    $('.NetAmount').text(Total);
    $('.SubTotal').text(SubTotal);
    $('.TaxAmount').text(TaxAmount);
    $('.TotalDiscountAmount').text(TotalDiscountAmount);

    $('#SubTotal').val(SubTotal);
    $('#TaxAmount').val(TaxAmount);
    $('#NetAmount').val(Total);
    $('#TotalDiscountAmount').val(TotalDiscountAmount);
}

function calculatenetAmount(element) {

    var Amount = $(element).val();
    var Taxes = $(element).closest('tr').find('.taxes').val();
    var taxpercentages = [];
    $(Taxes).each(function (index, element) {
        var val = GetTaxPercentage(element);
        taxpercentages.push(val);
    })

    var percentageincomma = taxpercentages.join(',');
    var Amount = $(element).closest('tr').find('.netamountCls').val();
    var netamount = Amount;
    $(taxpercentages).each(function (i, e) {
        tax = Math.round((Amount / 100 * e) * 100) / 100;
        netamount = Number(netamount) + Number(tax);
    })

    $(element).closest('tr').find('.netamount').val(Number(netamount).toFixed(2));
}

function CalculateRentalServiceAmount(element) {
    var fromDate = $(element).parent().parent().find('#fromDate').val();
    var toDate = $(element).parent().parent().find('#toDate').val();
    if (toDate != "" && Date.parse(toDate) < Date.parse(fromDate)) {
        Toast.show('ToDate should be greater than FromDate!', 'error', 5000);
        $(element).parent().parent().find('#toDate').val('');
        return;
    }

    var basePrice = parseFloat($(element).parent().parent().find('.basenetamountCls').val());
    if (isNaN(basePrice)) {
        console.error("Invalid base price");
        return;
    }

    var days = GetDatesDiff(fromDate, toDate);
    var FrequencyId = $('#FrequencyId').val();
    var newAmount = basePrice; // Default to basePrice

    if (days != 0 && FrequencyId == 1) {
        // Daily frequency
        newAmount = basePrice * days;
    }
    if (days != 0 && FrequencyId == 2) {
        newAmount = 0;
        var startDate = new Date(fromDate);
        var endDate = new Date(toDate);
        var currentMonthStart = new Date(startDate);
        currentMonthStart.setDate(1);

        // Loop through each month in the date range
        while (currentMonthStart <= endDate) {
            var daysInMonth = new Date(currentMonthStart.getFullYear(), currentMonthStart.getMonth() + 1, 0).getDate();

            // Determine the start and end of the stay in the current month
            var monthStart = currentMonthStart.getTime() < startDate.getTime() ? startDate : currentMonthStart;
            var monthEnd = new Date(currentMonthStart.getFullYear(), currentMonthStart.getMonth() + 1, 0);
            if (monthEnd > endDate) {
                monthEnd = endDate;
            }

            var daysInThisMonth = GetDatesDiff(monthStart, monthEnd);
            var monthlyRate = basePrice;
            var proratedAmount = (monthlyRate / daysInMonth) * daysInThisMonth;
            newAmount += proratedAmount;

            // Move to the next month
            currentMonthStart.setMonth(currentMonthStart.getMonth() + 1);
            currentMonthStart.setDate(1);
        }
    }
    if (days != 0 && FrequencyId == 3) {
        // Weekly frequency
        newAmount = (basePrice / 7) * days;
    }

    // Ensure newAmount is a valid number before setting the value
    if (!isNaN(newAmount)) {
        $(element).parent().parent().find('.netamountCls').val(newAmount.toFixed(2));
    } else {
        console.error("Calculated amount is invalid");
    }

    calculatenetAmount(element);
    CalculateAdjustmentAmount();
}

// Modify the existing Dateset function to prevent infinite loops when triggered programmatically
function Dateset() {
    $('.date').off('change').on('change', function (e, isTriggered) {
        var id = $(this).attr('id');
        var date1 = $(this).parent().parent().find('#fromDate').val();
        var date2 = $(this).parent().parent().find('#toDate').val();
        var FrequencyId = $("#FrequencyId").val();

        if (id == 'fromDate') {
            if (FrequencyId == '2') {
                var startDate = new Date(date1);
                var daysInMonth = new Date(startDate.getFullYear(), startDate.getMonth() + 1, 0).getDate();

                startDate.setDate(startDate.getDate() - 1 + daysInMonth);

                var formattedDate = startDate.toLocaleDateString('en-AE', {
                    day: '2-digit',
                    month: 'short',
                    year: 'numeric'
                }).replace(',', '');

                formattedDate = formattedDate.replace(/(\d{1,2})\s(\w{3})\s(\d{4})/, "$1/$2/$3");

                $(this).parent().parent().find('#toDate').datepicker("setDate", formattedDate);
            }
            if (FrequencyId == '3') {
                var startDate = new Date(date1);
                startDate = new Date(startDate.valueOf());
                startDate.setDate(startDate.getDate() + 7);
                var date = startDate.toISOString().split('T')[0];
                $(this).parent().parent().find('#toDate').val(date);
            }
        }

        CalculateRentalServiceAmount(this);

        // Only trigger cleaning fee synchronization if this is not already a triggered event
        if (!isTriggered) {
            syncCleaningDatesForRental(this);
        }
    });
}

function NaNToZero(val) {
    if (val == '') {
        return 0;
    }
    if (isNaN(val)) {
        return 0;
    }
    return val;
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

function GetTaxes(id) {

    var url = GetFormatedUrl("/Invoicings/GetTaxes")
    $.ajax({
        type: "Get",
        url: url,
        async: false,
        success: function (data) {
            var data = data.data;
            $("[name='" + id + "']").html('');
            var html = "";
            $.each(data, function (key, value) {
                html = html + "<option Value='" + value.Value + "'>" + value.Text + "</option>"
            })
            $("[name='" + id + "']").html(html);

        }
    });
}

function GetTaxesEdit() {

    var url = GetFormatedUrl("/Invoicings/GetTaxes")
    $.ajax({
        type: "Get",
        url: url,
        async: false,
        success: function (data) {
            var data = data.data;
            $(".taxes").html('');
            var html = "";
            $.each(data, function (key, value) {
                html = html + "<option Value='" + value.Value + "'>" + value.Text + "</option>"
            })
            $(".taxes").html(html);

        }
    });
}

function GetFrequency() {

    var studentId = $("#StudentId").val();
    var configId = 0;
    configId = $("#ConfigId").val();
    if (configId == null || configId == "" || configId == undefined) {
        var configId = $("#OccupancyId").val();
    }
    $.ajax({

        url: GetFormatedUrl('/invoicings/GetFrequencyById?Id=' + studentId + "&configId=" + configId),
        method: "Get",

        success: function (data) {
            if (data.status) {
                var res = data.data;
                $("#FrequencyId").val(res.FrequencyId);



            }
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

function ResetDetail() {

    $('.tbdyGRN').html('');
    counter = 0;
    addInvoiceRow();
    $('.btnDelete').parent().remove();

    $('.NetAmount').text(0.00);
    $('.SubTotal').text(0.00);
    $('.TaxAmount').text(0.00);
    $('.TotalDiscountAmount').text(0.00);
    $('#SubTotal').val(0);
    $('#TaxAmount').val(0);
    $('#NetAmount').val(0);
    $('#TotalDiscountAmount').val(0);
}

// Function to handle date synchronization between rental and cleaning fee rows
function syncCleaningFeeDates() {
    // When fromDate or toDate changes on any row
    $('.date').on('change', function () {
        var currentRow = $(this).closest('tr');
        var rowIndex = currentRow.index();
        var currentServiceText = currentRow.find('.items').val().toLowerCase();

        // Only proceed if this is a rental charges row
        if (currentServiceText.includes('rental')) {
            var fromDate = currentRow.find('#fromDate').val();
            var toDate = currentRow.find('#toDate').val();
            var nextRow = currentRow.next('tr');

            // Check if the next row exists and is a cleaning fee
            if (nextRow.length && nextRow.find('.items').val().toLowerCase().includes('cleaning')) {
                // Sync the dates
                nextRow.find('#fromDate').val(fromDate).trigger('change', [true]); // Pass a parameter to avoid infinite loop
                nextRow.find('#toDate').val(toDate).trigger('change', [true]);
            }
        }
    });
}

// New function to specifically sync dates from rental to cleaning fee
function syncCleaningDatesForRental(element) {
    var currentRow = $(element).closest('tr');
    var currentServiceText = currentRow.find('.items').val().toLowerCase();

    // Only proceed if this is a rental charges row
    if (currentServiceText.includes('rental')) {
        var fromDate = currentRow.find('#fromDate').val();
        var toDate = currentRow.find('#toDate').val();
        var nextRow = currentRow.next('tr');

        // Check if the next row exists and is a cleaning fee
        if (nextRow.length && nextRow.find('.items').val().toLowerCase().includes('cleaning')) {
            // Sync the dates
            nextRow.find('#fromDate').val(fromDate).trigger('change', [true]);
            nextRow.find('#toDate').val(toDate).trigger('change', [true]);
        }
    }
}

// Enhanced function to initialize date sync handlers
function initializeDateSyncHandlers() {
    // First unbind any existing handlers to prevent duplicates
    $('.date').off('change.dateSyncHandler');

    // Re-add handlers
    $('.date').on('change.dateSyncHandler', function (e, isTriggered) {
        // Skip if this is a programmatically triggered event to avoid loops
        if (isTriggered) return;

        // Only proceed for rental charge rows
        var currentRow = $(this).closest('tr');
        var currentServiceText = currentRow.find('.items').val().toLowerCase();

        if (currentServiceText && currentServiceText.includes('rental')) {
            var fromDate = currentRow.find('#fromDate').val();
            var toDate = currentRow.find('#toDate').val();
            var nextRow = currentRow.next('tr');

            // Check if next row is a cleaning fee
            if (nextRow.length && nextRow.find('.items').val().toLowerCase().includes('cleaning')) {
                // Update cleaning fee dates and make them readonly and disabled
                nextRow.find('#fromDate').val(fromDate)
                    .trigger('change', [true])
                    .prop('readonly', true)
                    .prop('disabled', true)
                    .addClass('disabled-date-field');

                nextRow.find('#toDate').val(toDate)
                    .trigger('change', [true])
                    .prop('readonly', true)
                    .prop('disabled', true)
                    .addClass('disabled-date-field');
            }
        }
    });

    // Apply readonly and disabled state to all cleaning fee date fields
    makeCleaningFeeDateFieldsReadonly();
    makeCleaningFeeDiscountFieldsReadonly();

}

function makeCleaningFeeDiscountFieldsReadonly() {
    $('.parentRowGrider').each(function () {
        const rowText = $(this).find('.items').val().toLowerCase();

        if (rowText && rowText.includes('cleaning')) {
            $(this).find('.discountPercentageCls').prop('readonly', true)
                .prop('disabled', true)
                .addClass('disabled-discount-field');
        }
    })
}
// Improved function to make cleaning fee date fields fully readonly and disabled
function makeCleaningFeeDateFieldsReadonly() {
    // Loop through all rows
    $('.parentRowGrider').each(function () {
        const rowText = $(this).find('.items').val().toLowerCase();

        // If this is a cleaning fee row
        if (rowText && rowText.includes('cleaning')) {
            // Make date fields readonly, disabled, and add a visual class
            $(this).find('#fromDate, #toDate').prop('readonly', true)
                .prop('disabled', true)
                .addClass('disabled-date-field');
        }
        else {
            $(this).find('#fromDate, #toDate').prop('readonly', false)
                .prop('disabled', false)
                .removeClass('disabled-date-field');
        }
    });
}

function SaveInvoice(Approve) {
    ShowLoader();
    $('.savebtn, .saveandapprovebtn')
        .prop("disabled", true)
        .html('<i class="fa fa-2x fa-sync-alt fa-spin  fa-refresh loading "></i>')
        .css('background', '#016891 !important');

    var Id = $('#Id').val();
    var LocationId = $('#LocationId').val();
    var InvoiceTypeId = $('#InvoiceTypeId').val();
    var code = $('#Code').val();
    var SubTotal = $('#SubTotal').val();
    var TotalDiscountAmount = $('#TotalDiscountAmount').val();
    var NetAmount = $('#NetAmount').val();
    var Student = $('#StudentId').val();
    var InvoiceDate = $('#datepicker').val();
    //var DueDate = $('#dueDatePicker').val();
    var Remarks = $('#Remarks').val();
    var TaxIdsSel = $('.taxes').val();
    var TaxIds = Array.isArray(TaxIdsSel) ? TaxIdsSel.join(',') : '';
    var CreatedDate = $('#CreatedDate').val();
    var termId = $('#OccupancyId').val();
    var Taxamout = $('#TaxAmount').val();

    if (LocationId == null || LocationId == '' || LocationId == undefined) {
        showMessage('Please Select Location!');
        $('.savebtn')
            .prop("disabled", false)
            .html('')
            .text('Save');

        $('.saveandapprovebtn')
            .prop("disabled", false)
            .html('')
            .text('Save & Approve')
        HideLoader();
        return;
    }

    if (Student == null || Student == '' || Student == undefined) {
        showMessage('Please Select Resident id!');
        $('.savebtn')
            .prop("disabled", false)
            .html('')
            .text('Save');
        $('.saveandapprovebtn')
            .prop("disabled", false)
            .html('')
            .text('Save & Approve')
        HideLoader();
        return;
    }

    if (InvoiceDate == null || InvoiceDate == '' || InvoiceDate == undefined) {
        showMessage('Please Select Invoice Date');
        $('.savebtn')
            .prop("disabled", false)
            .html('')
            .text('Save');

        $('.saveandapprovebtn')
            .prop("disabled", false)
            .html('')
            .text('Save & Approve')
        HideLoader();
        return;
    }
    // Validate DueDate
    //if (DueDate == null || DueDate == '' || DueDate == undefined) {
    //    showMessage('Due Date is not set');
    //    $('.savebtn')
    //        .prop("disabled", false)
    //        .html('')
    //        .text('Save');

    //    $('.saveandapprovebtn')
    //        .prop("disabled", false)
    //        .html('')
    //        .text('Save & Approve');
    //    return;
    //}

    var invoicing = {
        Id: Id,
        InvoiceDate: InvoiceDate,
        //DueDate: DueDate,
        StudentId: Student,
        Remarks: Remarks,
        TotalPrice: SubTotal,
        TaxIds: TaxIds,
        CreatedDate: CreatedDate,
        TaxAmount: Taxamout,
        NetAmount: NetAmount,
        LocationId: LocationId,
        InvoiceTypeId: InvoiceTypeId,
        Code: code,
        IsApproved: Approve,
        TermID: termId,
        TotalDiscountAmount: TotalDiscountAmount
    }

    var Details = [];
    var empity = false;

    $("#table1 .items").each(function (index, element) {
        var discountPercentage = $(element).parent().parent().find('.discountPercentageCls').val();
        var Price = $(element).parent().parent().find('.netamountCls').val();
        Price = Price == "" ? 0 : Price;
        var DiscountAmount = Math.round((Price * (discountPercentage / 100)) * 100) / 100;
        var Desc = $(element).parent().parent().find('.Desc').val();
        var fromDate = $(element).parent().parent().find('#fromDate').val();
        var toDate = $(element).parent().parent().find('#toDate').val();

        if ($('#InvoiceTypeId').val() == 3) {
            var invoiceDate = new Date($("#datepicker").val());
            //fromDate = new Date(invoiceDate.getFullYear(), invoiceDate.getMonth(), 1).toDateString();
            //toDate = new Date(invoiceDate.getFullYear(), invoiceDate.getMonth() + 1, 0).toDateString();
            fromDate = null;
            toDate = null;
        }

        var taxSel = $(element).parent().parent().find('.taxes').val();
        var taxids = Array.isArray(taxSel) ? taxSel.join(',') : '';
        var taxName = $(element).parent().parent().find(".taxes").select2('data') || [];
        var taxesNames = [];
        $(taxName).each(function (i, e) {
            var c = e.text;
            taxesNames.push(c);
        })

        var Taxtext = taxesNames.join(', ');
        var TotalAmount = $(element).parent().parent().find('.netamount').val();
        var TaxAmount = Math.round(((Number(TotalAmount) + Number(DiscountAmount)) - Number(Price)) * 100) / 100;
        var item = $(this).attr('data-id');
        var itemName = $(this).val();

        if (item == null || item == '' || item == undefined) {
            showMessage('Please select service!');
            $(this).addClass('highlight');
            empity = true;
            return;
        }

        if ($('#InvoiceTypeId').val() == 1) {
            if (fromDate == null || fromDate == '') {
                empity = true;
                $(element).parent().parent().find('#fromDate').addClass('highlight')
                return;
            }

            if (toDate == null || toDate == '') {
                empity = true;
                $(element).parent().parent().find('#toDate').addClass('highlight')
                return;
            }
        }

        var InvoiceDetail = {
            ServiceId: item,
            ServiceName: itemName,
            Price: Price,
            Description: Desc,
            TaxesIds: taxids,
            TaxesName: Taxtext,
            TaxAmount: TaxAmount,
            TotalAmount: TotalAmount,
            FromDate: fromDate,
            ToDate: toDate,
            DiscountPercentage: discountPercentage,
            DiscountAmount: DiscountAmount
        };

        Details.push(InvoiceDetail);
    });

    if (empity == true) {
        $('.savebtn')
            .prop("disabled", false)
            .html('')
            .text('Save');

        $('.saveandapprovebtn')
            .prop("disabled", false)
            .html('')
            .text('Save & Approve')
        HideLoader();
        return;
    }

    $.ajax({
        type: "Post",
        url: GetFormatedUrl('/Invoicings/SaveInvoice'),
        async: true,
        data: {
            invoicing: invoicing,
            list: Details
        },
        success: function (data) {
            if (data.status == true) {
                window.location.replace(GetFormatedUrl('/Invoicings/Index'));
            } else {
                HideLoader();
                //$('.savebtn, .saveandapprovebtn')
                //    .prop("disabled", false)
                //    .html('');
                //window.location.replace(GetFormatedUrl('/Invoicings/Index'));



                showMessage(data.error || 'Some error occurred!');
                $('.savebtn')
                    .prop("disabled", false)
                    .html('')
                    .text('Save');

                $('.saveandapprovebtn')
                    .prop("disabled", false)
                    .html('')
                    .text('Save & Approve')
                return;


                //showMessage(data.error || 'Some error occurred!');
            }
        },
        error: function (data) {
            HideLoader();
            $('.savebtn, .saveandapprovebtn')
                .prop("disabled", false)
                .html('');
            showMessage(data.error || 'Some error occurred!');
        }
    });
    //window.location.replace(GetFormatedUrl('/Invoicings/Index'))
}



