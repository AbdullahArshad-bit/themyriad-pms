$(function () {
    $('.select2').select2();


    $('.fa-arrow-circle-left').parents('.btn-themyriad').addClass('table-panel-cst');
    $('.table-panel-cst').each(function (index, elem) {
        var buttonToAdd = $(this).attr('href');
        $('.confirmGoBack').attr('onclick', "window.location.href='" + buttonToAdd + "'");
        $(this).attr('href', '#');
        $(this).attr('onclick', 'OpenCancelModal();');
    })
})


function OpenCancelModal() {
    $('#CancelBtnConfirm').modal('show');

}


$('body').on('keyup','.decimals', function () {
    var val = $(this).val();
    if (isNaN(val)) {
        val = val.replace(/[^0-9\.]/g, '');
        if (val.split('.').length > 2)
            val = val.replace(/\.+$/, "");
    }
    $(this).val(val);
});


function GetFormatedUrl(url) {
    var browseurl = window.location.href.toLowerCase();
    var isTrue = browseurl.includes("".toLowerCase());
    var isExist = url.includes("".toLowerCase());
    if (isTrue === true && isExist === false) {
        url = "/" + url;
    }
    return url;
}

/**
 * get value from query string.
 * Defined On: Common.js
 * @param {string} name 
 */
function GetQueryStringValue(name) {
    var url = window.location.href.slice(window.location.href.indexOf('?') + 1).split('&');
    for (var i = 0; i < url.length; i++) {
        var urlparam = url[i].split('=');
        if (urlparam[0] == name) {
            return urlparam[1];
        }
    }
}
//$("input[type=submit]").click(function (e) {

//    
//    var allFieldsFilled = true;
//    $("[required]").each(function () {
//        if ($(this).val().trim() === '') {
//            allFieldsFilled = false;

//            return false;
//        }
//    });
//    if (allFieldsFilled) {
//        ShowLoader();
//    }
//});
//$("button[type=button]").click(function (e) {

//    
//    var allFieldsFilled = true;
//    $("[required]").each(function () {
//        if ($(this).val().trim() === '') {
//            allFieldsFilled = false;

//            return false;
//        }
//    });
//    if (allFieldsFilled) {
//        ShowLoader();
//    }
//});

function ShowLoader() {
    $('.Customoverlay').css('display', 'block'); // Hide the custom loader
    $('.Customloader').css('display', 'block');

}
function HideLoader() {
    $('.Customoverlay').css('display', 'none'); // Hide the custom loader
    $('.Customloader').css('display', 'none'); // Hide the custom loader
}

