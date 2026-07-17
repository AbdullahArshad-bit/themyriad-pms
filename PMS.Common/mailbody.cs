using PMS.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace PMS.Common
{
    public class mailbody
    {
        private static string emailSmtpHost = "smtppro.zoho.com";

        public static string EmailSmtpHost
        {
            get { return emailSmtpHost; }
            set { emailSmtpHost = value; }
        }

        private static int emailSmtpPort = 587;

        public static int EmailSmtpPort
        {
            get { return emailSmtpPort; }
            set { emailSmtpPort = value; }
        }

        private static string fromEmailMuscat = "bookings.muscat@themyriad.com";

        public static string FromEmailMuscat
        {
            get { return fromEmailMuscat; }
            set { fromEmailMuscat = value; }
        }

        private static string fromEmail = "register@themyriad.com";

        public static string FromEmail
        {
            get { return fromEmail; }
            set { fromEmail = value; }
        }

        private static string fromEmailMuscatPassword = "Shgc@2021";

        public static string FromEmailMuscatPassword
        {
            get { return fromEmailMuscatPassword; }
            set { fromEmailMuscatPassword = value; }
        }

        private static string fromEmailPassword = "Shgc@2021";

        public static string FromEmailPassword
        {
            get { return fromEmailPassword; }
            set { fromEmailPassword = value; }
        }



        private static string emailDisplayName = "The Myriad Dubai";

        public static string EmailDisplayName
        {
            get { return emailDisplayName; }
            set { emailDisplayName = value; }
        }
        private static string emailMuscatDisplayName = "The Myriad";

        public static string EmailMuscatDisplayName
        {
            get { return emailMuscatDisplayName; }
            set { emailMuscatDisplayName = value; }
        }



        private static string myriadAdminEmail = "info@themyriad.com";

        public static string MyriadAdminEmail
        {
            get { return myriadAdminEmail; }
            set { myriadAdminEmail = value; }
        }


        private static string myriadmuscatBookingAdminEmail = "bookings.muscat@themyriad.com";

        public static string MyriadMuscatBookingAdminEmail
        {
            get { return myriadmuscatBookingAdminEmail; }
            set { myriadmuscatBookingAdminEmail = value; }
        }

        private static string myriadBookingAdminEmail = "bookings@themyriad.com";

        public static string MyriadBookingAdminEmail
        {
            get { return myriadBookingAdminEmail; }
            set { myriadBookingAdminEmail = value; }
        }

        private static string myriadBookingCCEmail = "leasing@dxb.themyriad.com,support@dxb.themyriad.com,klara@strategichousinggroup.com";

        public static string MyriadBookingCCEmail
        {
            get { return myriadBookingCCEmail; }
            set { myriadBookingCCEmail = value; }
        }

        private static string myriadmuscatBookingCCEmail = "klara@strategichousinggroup.com,leasing.muscat@themyriad.com";

        public static string MyriadMuscatBookingCCEmail
        {
            get { return myriadmuscatBookingCCEmail; }
            set { myriadmuscatBookingCCEmail = value; }
        }

        public static string BankTransferBooking(EF.Booking booking, EF.V_RoomTypePriceDetail priceDetail, string emailMessageBody)
        {
            string body = "";

            //    //var path = HttpContext.Current.Request.MapPath("~/Views/EmailTemplates/muscat_booking_bank_transfer.html");

            //    var path = 
            //    body = Helper.ReadFile(path);


            //body = System.IO.File.ReadAllText(path);
            var Request = HttpContext.Current.Request;
            body = emailMessageBody;
            try
            {
                if (string.IsNullOrEmpty(body))
                    return "";
                string checkIn = "";
                if (booking.CheckInDate.Year < DateTime.Today.Year)
                    checkIn = "N/A";
                else
                    checkIn = booking.CheckInDate.ToString("dd-MMM-yyyy");

                string checkOut = "";
                if (booking.CheckOutDate.HasValue)
                {
                    if (booking.CheckOutDate.Value.Year < DateTime.Today.Year)
                    {
                        checkOut = "N/A";
                    }
                    else
                    {
                        checkOut = booking.CheckOutDate.Value.ToString("dd-MMM-yyyy");
                    }
                }
                else
                {
                    checkOut = "N/A";
                }

                decimal? Price = priceDetail.Price;
                decimal? TotalCommitmentPrice = priceDetail.TotalCommitmentPrice;
                decimal? Deposit = priceDetail.InitialDeposit;
                var encryptUrl = Convert.ToBase64String(Encoding.UTF8.GetBytes(booking.PersonID + "," + booking.BookingID));


                body = body.Replace("_room", priceDetail.Name.ToUpper());
                body = body.Replace("_name", booking.Person.FullName.ToUpper());
                body = body.Replace("_email", booking.Person.Email.ToLower());
                body = body.Replace("_phone", booking.Person.Phone);
                body = body.Replace("_bank", priceDetail.Bank);
                body = body.Replace("_branch", priceDetail.Branch);
                body = body.Replace("_title", priceDetail.Title);
                body = body.Replace("_account", priceDetail.Account);
                body = body.Replace("_currency", priceDetail.Currency);
                body = body.Replace("_swift_code", priceDetail.SwiftCode);
                body = body.Replace("_CheckIn", checkIn);
                body = body.Replace("_CheckOut", checkOut);

                body = body.Replace("{{ConfirmationLink}}", Request.Url.GetLeftPart(UriPartial.Authority) + Request.ApplicationPath + "Booking/UploadReceipt?encIds=" + encryptUrl);

                body = body.Replace("_commitment_amount", priceDetail.Currency + " " + String.Format("{0:n0}", TotalCommitmentPrice));
                body = body.Replace("_payable_amount", priceDetail.Currency + " " + String.Format("{0:n0}", Deposit));
                body = body.Replace("_booking_number", booking.BookingNumber.ToUpper());
                if (priceDetail.TermName.ToLower().Contains("daily"))
                {
                    var days = (booking.CheckOutDate - booking.CheckInDate).Value.Days;
                    body = body.Replace("_per_month_amount", priceDetail.Currency + " " + String.Format("{0:n0}", ((Price * 1) / 1)) + " x " + days);

                    body = body.Replace("_Commitment", "DAILY");
                }
                else
                {
                    body = body.Replace("_per_month_amount", priceDetail.Currency + " " + String.Format("{0:n0}", ((Price * 1) / 1)) + " x " + 1);
                    body = body.Replace("_Commitment", "MONTHLY");
                }
                //body = body.Replace("_per_month_amount", priceDetail.Currency +" " + String.Format("{0:n0}", ((Price * 1) / 1)) + " x " + priceDetail.DurationType);
                //body = body.Replace("_Commitment", priceDetail.DurationType + " month(s)");

                return body;
            }
            catch (Exception exe)
            {


            }

            return body;
        }
        public static string CreditCardBooking(EF.Booking booking, EF.V_RoomTypePriceDetail priceDetail, string emailMessageBody)
        {
            string body = "";

            {
                //var path = HttpContext.Current.Server.MapPath("/Views/EmailTemplates/booking_credit_card_muscat.html");
                //body = Helper.ReadFile(path);

                //body = System.IO.File.ReadAllText(path);
                body = emailMessageBody;

                if (string.IsNullOrEmpty(body))
                    return "";

                string checkIn = "";
                if (booking.CheckInDate.Year < DateTime.Today.Year)
                    checkIn = "N/A";
                else
                    checkIn = booking.CheckInDate.ToString("dd-MMM-yyyy");

                string checkOut = "";
                if (booking.CheckOutDate.HasValue)
                {
                    if (booking.CheckOutDate.Value.Year < DateTime.Today.Year)
                    {
                        checkOut = "N/A";
                    }
                    else
                    {
                        checkOut = booking.CheckOutDate.Value.ToString("dd-MMM-yyyy");
                    }
                }
                else
                {
                    checkOut = "N/A";
                }

                decimal? Price = priceDetail.Price;
                decimal? TotalCommitmentPrice = priceDetail.TotalCommitmentPrice;
                decimal? Deposit = priceDetail.InitialDeposit;


                body = body.Replace("_reference_number", booking.TranRef.ToUpper());
                body = body.Replace("_paid_amount", priceDetail.Currency + " " + String.Format("{0:n0}", Convert.ToInt32(priceDetail.InitialDeposit)));
                body = body.Replace("_card_number", booking.CardLastDigits.ToUpper());

                body = body.Replace("_room", priceDetail.Name.ToUpper());
                body = body.Replace("_name", booking.Person.FullName.ToUpper());
                body = body.Replace("_email", booking.Person.Email.ToLower());
                body = body.Replace("_phone", booking.Person.Phone);
                body = body.Replace("_CheckIn", checkIn);
                body = body.Replace("_CheckOut", checkOut);
                body = body.Replace("_commitment_amount", priceDetail.Currency + String.Format("{0:n0}", TotalCommitmentPrice));
                body = body.Replace("_payable_amount", priceDetail.Currency + String.Format("{0:n0}", Deposit));
                body = body.Replace("_booking_number", booking.BookingNumber.ToUpper());

                if (priceDetail.TermName.ToLower().Contains("daily"))
                {
                    var days = (booking.CheckOutDate - booking.CheckInDate).Value.Days;
                    body = body.Replace("_per_month_amount", priceDetail.Currency + " " + String.Format("{0:n0}", ((Price * 1) / 1)) + " x " + days);

                    body = body.Replace("_Commitment", "DAILY");
                }
                else
                {
                    body = body.Replace("_per_month_amount", priceDetail.Currency + " " + String.Format("{0:n0}", ((Price * 1) / 1)) + " x " + 1);
                    body = body.Replace("_Commitment", "MONTHLY");
                }
                //body = body.Replace("_per_month_amount", "RO " + String.Format("{0:n0}", ((Price * 1) / 1)) + " x " + 1);
                //body = body.Replace("_Commitment", priceDetail.DurationType + " month(s)");

            }



            return body;
        }


        public static string RefundRequest(EF.RefundRequest refundRequest, string emailMessageBody)
        {
            string body = "";

            body = emailMessageBody;
            try
            {

                body = body.Replace("_full_name", refundRequest.Person.FullName);
                body = body.Replace("_myriad_id", refundRequest.MyriadID);
                body = body.Replace("_email", refundRequest.Email);
                body = body.Replace("_phone", refundRequest.Person.Phone);
                body = body.Replace("_accountnumber", refundRequest.AccountNumber);
                body = body.Replace("_bankaccount", refundRequest.BankAccount);
                body = body.Replace("_signature", refundRequest.Signature);

                return body;
            }
            catch (Exception exe)
            {


            }

            return body;
        }

    }
}
