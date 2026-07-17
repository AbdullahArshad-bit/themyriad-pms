$(function () {
    $('.data-table').DataTable({});
})
var generadtedInspectionID = 0;
var ID = 0;

function OpenConfirmInspectionModal(id) {

    ID = id;
    $('#GeneratedInspectionConfirm').modal('show');
}

function EditInspection() {

    var obj = new Object();

   

    if ($("#Status :selected").val() == "" || $('#Status').val() == 0 || $('#Status').val() == undefined) {


        $('#Status').addClass('highlight');
        showMessage('Please Select Status Value!');

        return


    }
    else {
        $('#Status').removeClass('highlight');
    }

 

    if ($('#StaffStatus :selected').val() == "" || $('#StaffStatus').val() == 0 || $('#StaffStatus').val() == undefined) {


        $('#StaffStatus').addClass('highlight');
        showMessage('Please Select Status Value!');

        return


    }
    else {
        $('#StaffStatus').removeClass('highlight');
    }

    if ($('#StudentStatus :selected').val() == "" || $('#StudentStatus').val() == 0 || $('#StudentStatus').val() == undefined) {


        $('#StudentStatus').addClass('highlight');
        showMessage('Please Select Status Value!');

        return


    }
    else {
        $('#StudentStatus').removeClass('highlight');

    }

    obj.ID = ID;
    obj.InspectionID = generadtedInspectionID;
    obj.Maintenance_Status = $('#Status').val();
    obj.Student_Status = $('#StudentStatus').val();
    obj.Staff_Status = $('#StaffStatus').val();
    obj.Maintenance_Remarks = $('#Remarks').val();


    var data = JSON.stringify(obj);

    $.ajax({

        url: GetFormatedUrl("/Inspection/EditGenerateNewInspection"),
        type: "POST",
        data: data,
        dataType: "json",
        contentType: "application/json;charset=UTF-8",

        success: function (data) {
            if (data.Success != 'true' && data.Code == 404) {

                return;
            }


            if (data.data != null) {

                $('#Field_Body').html('');
                $('#Field_Body1').html('');

                $(".modal .close").click();
                $('#InspectionConfirm').modal('show');



                var HTML_FIELD =
                    '<div class="row col-sm-12">' +
                    '<div class="row col-sm-4">' +
                    '<label style="color:#c20e1a;"> Inspection Name : </label>' +
                    '</div>' +
                    '<div class="row col-sm-6">' +
                    '<label id="' + data.data.GeneratedInspectionID + '" class="GeneratedInspectionID">' + data.data.inspectionName + '</label>' +
                    '</div>' +
                    '</div>' +

                    '<div class="col-md-12">' +
                    '<div class="row col-sm-4">' +
                    '<label style="color:#c20e1a;"> Bed Space Name : </label>' +
                    '</div>' +
                    '<div class="row col-sm-6">' +
                    '<label>' + data.data.BedSpace_Name + '</label>' +
                    ' </div>' +
                    '</div>'


                $('#Field_Body').append(HTML_FIELD);

                var index = 0;
                var InpectionFieldshtml = "";
                $.each(data.data.Fileds, function (i, e) {


                    HTML_FIELD =
                        '<div class="row col-sm-12 " >' +
                        '<div class="row col-sm-3">' +
                        '<label style="color:#c20e1a;margin-top:10px;" id="' + e.FieldID + '" class="Detail[' + index + '][GeneratedInspectionField]">' + e.InspectionFieldName + ' :-</label>' +
                        '</div>' +
                        '</div>'
                    InpectionFieldshtml = InpectionFieldshtml + HTML_FIELD;


                    $.each(e.ratingLists, function (i, f) {

                        HTML_FIELD =
                            '<div class="col-md-12 no-l-padding items" >' +
                            '<div class="row col-sm-2 no-l-padding">' +
                            '<label data-show-id="' + e.FieldID + '" id="' + f.RatingID + '" class="RatingIDAndFieldID">' + f.RatingName + '</label>' +
                            '</div>' +
                            ' <div class="row col-sm-3 ">' +
                            //'<input type="file" name="ImageData" class="ImageGet "/>' +
                            '<input type="file" id="FileUpload1" multiple />' +
                            '<input type="button" id="btnUpload" value="Upload Files" /> ' +
                            ' </div>' +
                            '<div class="col-md-3">' +
                            '<input type="text"  class="Note" placeholder="Enter Note" name="fname">' +
                            '</div>' +
                            ' <div class="row col-sm-4 no-l-padding">' +
                            '<select  class="Ratingdropdown form-control" name="' + f.RatingName + '" id="exampleFormControlSelect1">' +
                            '<option value="0"> -- Select Rating Field -- </option>'

                        InpectionFieldshtml = InpectionFieldshtml + HTML_FIELD;
                        $.each(f.actualRatingLists, function (i, g) {

                            HTML_FIELD =
                                '<option  value="' + g.RatingListItemDetailID + '">' + g.RatingListItemName + '</option >'

                            InpectionFieldshtml = InpectionFieldshtml + HTML_FIELD;
                        });
                        HTML_FIELD =
                            '</select>' +
                            '</div>' +
                            '</div>'

                        InpectionFieldshtml = InpectionFieldshtml + HTML_FIELD;

                        index = index + 1;
                    });

                    InpectionFieldshtml = InpectionFieldshtml + HTML_FIELD;


                });

                $('#Field_Body1').append(InpectionFieldshtml);

            }
        }
    })
}

function SaveInspectionReport() {
    var count = 0;
    //$('.SaveInspectionReport')
    //    .prop("disabled", true)
    //    .html('<i class="fa fa-2x fa-sync-alt fa-spin  fa-refresh loading "></i>')
    //    .css('background', '#016891 !important');

    var generatedInspectionID = $('.GeneratedInspectionID').attr('id');
    var Details = [];
    $(".items").each(function (index, element) {
        var RatingID = $(element).find('.RatingIDAndFieldID').attr('id');
        var FieldID = $(element).find('.RatingIDAndFieldID').attr('data-show-id');

        var note = $(element).find('.Note').val();


        var imageurl = $(element).find('.ImageGet').val();
        var ratingdropdown = $(element).find('.Ratingdropdown').val();

        if (ratingdropdown == null || ratingdropdown == 0 || ratingdropdown == undefined) {


            $(element).find('.Ratingdropdown').addClass('highlight');
            count++;




        }
        else {
            $(element).find('.Ratingdropdown').removeClass('highlight');


        }


        var InspectionDetails = {
            AssignedFieldID: FieldID,
            ratingListID: RatingID,
            GeneratedInspectionID: generatedInspectionID,
            SelectetRatinglistitemID: ratingdropdown,
            RatingNote: note,
            RatingimageUrl: imageurl,
            IsEnable: true,
            IsActive: true
        };

        Details.push(InspectionDetails);



    });

    //
    if (count > 0) {
        showMessage('Please Select Rating Value!');
        return;
    }




    $.ajax({
        type: "Post",
        url: GetFormatedUrl('/Inspection/ADDInspectionDetails'),
        async: false,
        data: {
            Model: Details
        },
        success: function (data) {
            if (data.status == true) { }

            window.location.replace(GetFormatedUrl('/Inspection/GeneratedInspections'));
        },
        error: function (data) {

            showMessage('Some error occoured!');
        }
    });



}

$(document).ready(function () {
    $('#btnUpload').click(function () {


        // Checking whether FormData is available in browser  
        if (window.FormData !== undefined) {

            var fileUpload = $("#FileUpload1").get(0);
            var files = fileUpload.files;

            // Create FormData object  
            var fileData = new FormData();

            // Looping over all files and add it to FormData object  
            for (var i = 0; i < files.length; i++) {
                fileData.append(files[i].name, files[i]);
            }

            // Adding one more key to FormData object  
            //fileData.append('username', 'Manas');

            $.ajax({
                url: '/Inspection/UploadFiles',
                type: "POST",
                contentType: false, // Not to set any content header
                processData: false, // Not to process data
                data: fileData,
                success: function (result) {
                    alert(result);
                },
                error: function (err) {
                    alert(err.statusText);
                }
            });
        } else {
            alert("FormData is not supported.");
        }
    });
});

function OpenConfirmDeletenModal(ID) {

    id = ID;

    $('#confirm-delete').modal('show');

}


function DeleteGenerateInspection() {
    
    $.ajax({

        url: GetFormatedUrl("/Inspection/DeleteGenerateInspection?id=" + id),
        type: "GET",

        dataType: "json",
        contentType: "application/json;charset=UTF-8",

        success: function (data) {
            if (data.Success != 'true' && data.Code == 404) {

                return;
            }
            if (data.success != 'false') {
                window.location.href = GetFormatedUrl("/Inspection/GeneratedInspections");

            }
        }
    })


}



