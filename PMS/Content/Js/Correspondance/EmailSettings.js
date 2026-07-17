setTimeout(function () {
    $('#attrName').rules('add', { required: true });
}, 0);
var SenderId = 0;
function OpenTestEmailPopup(id) {
    SenderId = id;
    $('#ToEmail').val('');
    $('#Subject').val('');
    $('#TestEmailBody').val('');
    $('#EmailDeliveryResponse').text("");
    $('#TestEmailSettings').modal('show');
}
function SendTestEmail() {
    var SendTo = $('#ToEmail').val();
    var Subjecct = $('#Subject').val();
    var EmailBody = $('#TestEmailBody').val();
    if (!$("#TestEmailSettingsform").valid()) {
        $('.error').addClass('text-danger');
        return;
    }
    $.ajax({
        type: "Post",
        url: GetFormatedUrl('/Correspondence/SendTestEmail'),
        async: false,
        data: {
            ToEmail: SendTo,
            EmailBody: EmailBody,
            Subject: Subjecct,
            SenderEmail: SenderId
        },
        success: function (data) {
            var message = data.data;
            message.includes('Successfully') ? $('#EmailDeliveryResponse').addClass('text-success') : $('#EmailDeliveryResponse').addClass('text-danger');
            $('#EmailDeliveryResponse').text(message);
        },
        error: function (data) {
            $('.savebtn')
                .prop("disabled", false)
                .html('')
            showMessage('Some error occoured!');
        }
    });


}

$("#TestEmailSettingsform").validate({
    rules: {
        Subject: "required",
        ToEmail: { email: true, required: true },
        TestEmailBody: "required",

    },

    messages: {
        Subject: "Please enter subject",
        ToEmail: {
            email: "Enter Valid Email!",
            required: "Enter Email!"
        },
        TestEmailBody: {
            required: "Please enter mail body."
        },
    },
});
