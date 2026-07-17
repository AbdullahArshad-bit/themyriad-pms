using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using PMS.DTO.ViewModels.PersonManageViewModels;
using PMS.DTO.ViewModels.BookingViewModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.UI;
using PMS.EF;
using PMS.DTO.ViewModels.SetupViewModels;
using PMS.DTO.ViewModels.ApiViewModels;
using System.Globalization;
using PMS.DTO.ViewModels.BedSpacePlacementViewModels;
using PMS.Services.Services.LocationContext;

namespace PMS.Classes
{
    public class ExcelHelper
    {
        public List<AddPersonVM> GetPersonDataWithBooking(string filePath)
        {
            List<AddPersonVM> list = new List<AddPersonVM>();

            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                throw new FileNotFoundException("Excel file not found.", filePath);

            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var templateWorkbook = WorkbookFactory.Create(fileStream);
                    if (templateWorkbook == null)
                        return list;

                    var sheet = templateWorkbook.GetSheetAt(0);
                    if (sheet == null)
                        return list;

                    var headerRow = sheet.GetRow(sheet.FirstRowNum);
                    if (headerRow == null)
                        return list;

                    var headerIndex = GetHeaderIndexes(headerRow);
                    int lastRow = sheet.LastRowNum;
                    int firstDataRow = sheet.FirstRowNum + 1;
                    var assignedLocationIds = LocationContextService.GetAssignedLocationIdsStatic();
                    var defaultLocationId = assignedLocationIds != null && assignedLocationIds.Any()
                        ? assignedLocationIds.First()
                        : 0;

                    for (int i = firstDataRow; i <= lastRow; i++)
                    {
                        var row = sheet.GetRow(i);
                        if (row == null)
                            continue;

                        AddPersonVM m = new AddPersonVM();
                        AddBookingVM b = new AddBookingVM();

                        m.Title = GetCellValue(row, headerIndex, "title");
                        m.FullName = GetCellValue(row, headerIndex, "fullname");
                        m.Email = GetCellValue(row, headerIndex, "email");
                        m.Phone = GetCellValue(row, headerIndex, "phone");
                        m.PassportNumber = GetCellValue(row, headerIndex, "passportnumber");
                        m.Gender = GetCellValue(row, headerIndex, "gender");
                        m.Nationality = GetCellValue(row, headerIndex, "nationality");
                        m.UniversityName = GetCellValue(row, headerIndex, "university");
                        m.GuardianFullName = GetCellValue(row, headerIndex, "guardianfullname");
                        m.GuardianPhone = GetCellValue(row, headerIndex, "guardianphoneno");

                        m.GuardianOtherEmail = GetCellValue(row, headerIndex, "guardianemail");
                        m.GuardianRelation = GetCellValue(row, headerIndex, "relation");

                        var dob = ParseNullableDate(GetCellValue(row, headerIndex, "dob"));
                        if (dob.HasValue)
                            m.DOB = dob.Value;

                        b.TermName = GetCellValue(row, headerIndex, "term");
                        b.RoomTypeName = GetCellValue(row, headerIndex, "roomtype");
                        b.ImportPrice = ParseNullableDecimal(GetCellValue(row, headerIndex, "price"));

                        var checkInDate = ParseNullableDate(GetCellValue(row, headerIndex, "checkindate"));
                        if (checkInDate.HasValue)
                            b.CheckInDate = checkInDate.Value;

                        b.CheckOut = ParseNullableDate(GetCellValue(row, headerIndex, "checkoutdate"));
                        b.Requests = GetCellValue(row, headerIndex, "requests");
                        if (string.IsNullOrWhiteSpace(b.Requests))
                            b.Requests = GetCellValue(row, headerIndex, "request");

                        if (defaultLocationId > 0)
                            m.LocationId = defaultLocationId;

                        m.CreatedBy = PMS.Common.Globals.User.Email;
                        m.CreatedDate = DateTime.Now;

                        if (!string.IsNullOrWhiteSpace(m.FullName))
                        {
                            m.Booking = b;
                            list.Add(m);
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return list;
        }

        private Dictionary<string, int> GetHeaderIndexes(IRow headerRow)
        {
            var indexes = new Dictionary<string, int>();

            for (int j = headerRow.FirstCellNum; j < headerRow.LastCellNum; j++)
            {
                var headerValue = headerRow.GetCell(j)?.GetFormattedCellValue();
                var normalizedHeader = NormalizeImportKey(headerValue);
                if (!string.IsNullOrWhiteSpace(normalizedHeader) && !indexes.ContainsKey(normalizedHeader))
                    indexes.Add(normalizedHeader, j);
            }

            return indexes;
        }

        private string GetCellValue(IRow row, Dictionary<string, int> headerIndex, string key)
        {
            if (row == null || headerIndex == null)
                return string.Empty;

            if (!headerIndex.TryGetValue(NormalizeImportKey(key), out int index))
                return string.Empty;

            return row.GetCell(index)?.GetFormattedCellValue()?.Trim() ?? string.Empty;
        }

        private string NormalizeImportKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return new string(value
                .Trim()
                .ToLowerInvariant()
                .Where(char.IsLetterOrDigit)
                .ToArray());
        }

        private DateTime? ParseNullableDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (DateTime.TryParse(value, out DateTime parsedDate))
                return parsedDate;

            if (double.TryParse(value, out double oaDate))
            {
                try
                {
                    return DateTime.FromOADate(oaDate);
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        private decimal? ParseNullableDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var sanitizedValue = value.Replace(",", string.Empty).Trim();
            if (decimal.TryParse(sanitizedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsed))
                return parsed;

            if (decimal.TryParse(sanitizedValue, NumberStyles.Any, CultureInfo.CurrentCulture, out parsed))
                return parsed;

            return null;
        }
        public List<AddPersonVM> GetPersonData(string filePath)
        {
            List<AddPersonVM> list = new List<AddPersonVM>();

            try
            {
                // Getting the complete workbook...
                var templateWorkbook = new XSSFWorkbook(filePath);

                if (templateWorkbook != null)
                {
                    // Getting the worksheet by its name...
                    var sheet = templateWorkbook.GetSheetAt(0);

                    if (sheet != null)
                    {
                        // Getting the row... 0 is the first row.
                        int lastRow = sheet.LastRowNum;
                        int firstRow = lastRow > 0 ? 1 : 0; // Skip header row

                        for (int i = firstRow; i <= lastRow; i++)
                        {
                            AddPersonVM m = new AddPersonVM();

                            var row = sheet.GetRow(i);
                            if (row == null) continue; // Skip empty rows

                            for (int j = row.FirstCellNum; j < row.LastCellNum; j++)
                            {
                                var cell = row.GetCell(j);
                                if (cell != null)
                                {
                                    switch (j)
                                    {
                                        case 0: // MercuryID (The Myriad ID)
                                            m.MercuryID = cell.GetFormattedCellValue();
                                            break;
                                        case 4: // FullName
                                            m.FullName = cell.GetFormattedCellValue();
                                            break;
                                        case 5: // DOB
                                            if (cell != null)
                                            {
                                                if (cell.CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(cell))
                                                {
                                                    m.DOB = cell.DateCellValue; // Excel stores it as a date
                                                }
                                                else if (cell.CellType == CellType.String)
                                                {
                                                    DateTime dob;
                                                    if (DateTime.TryParseExact(cell.StringCellValue, new[] { "dd/MM/yyyy", "MM/dd/yyyy", "yyyy-MM-dd" },
                                                        CultureInfo.InvariantCulture, DateTimeStyles.None, out dob))
                                                    {
                                                        m.DOB = dob;
                                                    }
                                                }
                                            }

                                            break;
                                        case 6: // UniversityName
                                            m.UniversityName = cell.GetFormattedCellValue();
                                            break;
                                        case 8: // Gender
                                            m.Gender = cell.GetFormattedCellValue();
                                            // Set Title based on Gender
                                            m.Title = m.Gender.Equals("Male", StringComparison.OrdinalIgnoreCase) ? "Mr." : "Ms.";
                                            break;
                                        case 9: // Nationality
                                            m.Nationality = cell.GetFormattedCellValue();
                                            break;
                                        case 10: // Email
                                            m.Email = cell.GetFormattedCellValue();
                                            break;
                                        case 11: // Phone
                                            m.Phone = cell.GetFormattedCellValue();
                                            break;
                                        case 12: // University Student ID
                                            m.UniversityStudentID = cell.GetFormattedCellValue();
                                            break;
                                        case 15: // Profile Notes
                                            m.ProfileNotes = cell.GetFormattedCellValue();
                                            break;
                                        case 18: // Passport Number
                                            m.PassportNumber = cell.GetFormattedCellValue();
                                            break;
                                        case 21: // Guardian Full Name
                                            m.GuardianFullName = cell.GetFormattedCellValue();
                                            break;
                                        case 22: // Guardian Phone
                                            m.GuardianPhone = cell.GetFormattedCellValue();
                                            break;
                                    }
                                }
                            }

                            // Set default values
                            m.CreatedBy = PMS.Common.Globals.User.Email;
                            m.CreatedDate = DateTime.Now;

                            var assignedLocationIds = LocationContextService.GetAssignedLocationIdsStatic();

                            // Get the first valid location ID from the session or assigned locations
                            if (assignedLocationIds != null && assignedLocationIds.Any())
                            {
                                m.LocationId = assignedLocationIds.First();
                            }


                            // Validate required fields
                            if (!string.IsNullOrEmpty(m.FullName))
                            {
                                list.Add(m);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exception (log or rethrow)
            }

            return list;
        }



        public List<AddBookingVM> GetBookingData(string filePath)
        {
            List<AddBookingVM> list = new List<AddBookingVM>();
            PMSEntities db = new PMSEntities();

            try
            {
                // Getting the complete workbook...
                var templateWorkbook = new XSSFWorkbook(filePath);

                if (templateWorkbook != null)
                {
                    // Getting the worksheet by its name...
                    //var sheet = templateWorkbook.GetSheet("Sheet1");
                    var sheet = templateWorkbook.GetSheetAt(0);

                    if (sheet != null)
                    {
                        // Getting the row... 0 is the first row.
                        //var dataRow = sheet.GetRow(4);
                        int lastRow = sheet.LastRowNum;
                        //int firstRow = sheet.FirstRowNum;
                        int firstRow = lastRow > 0 ? 1 : 0;


                        for (int i = firstRow; i <= lastRow; i++)
                        {
                            AddBookingVM b = new AddBookingVM();

                            var row = sheet.GetRow(i);
                            for (int j = row.FirstCellNum; j < row.LastCellNum; j++)
                            {
                                var cell = row.GetCell(j);
                                if (cell != null)
                                {
                                    if (j == 0)
                                    {


                                        var mercuryID = cell.GetFormattedCellValue();
                                        var personid = db.People.Where(x => x.MercuryID == mercuryID).FirstOrDefault();
                                        b.PersonID = personid != null ? personid.PersonID : 0;
                                        b.MercuryID = personid != null ? personid.MercuryID : mercuryID;

                                    }
                                    else if (j == 1)
                                    {
                                        // TermName handling
                                        var termName = cell.GetFormattedCellValue();

                                        // Find TermID from the Term table using Term Name
                                        var term = db.Terms.Where(x => x.TermDescription == termName || x.TermName == termName).FirstOrDefault();

                                        if (term != null)
                                        {
                                            var termID = term.TermID;

                                            // Find PriceConfigID from PriceConfig table using the TermID
                                            var priceConfig = db.PriceConfigs.Where(x => x.TermID == termID).FirstOrDefault();

                                            if (priceConfig != null)
                                            {
                                                b.PriceConfigID = priceConfig.PriceConfigID;
                                            }
                                            else
                                            {
                                                // Handle case when PriceConfig is not found
                                                // You can log an error or set a default value
                                                b.PriceConfigID = 0; // or any other default value
                                            }
                                        }
                                        else
                                        {
                                            // Handle case when Term is not found
                                            // You can log an error or set a default value
                                            b.PriceConfigID = 0; // or any other default value
                                        }
                                    }

                                    else if (j == 2) // CheckInDate column
                                    {
                                        if (cell != null)
                                        {
                                            if (cell.CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(cell))
                                            {
                                                // Convert numeric value (Excel serial date) to DateTime
                                                b.CheckInDate = DateTime.FromOADate(cell.NumericCellValue);
                                            }
                                            else
                                            {
                                                string cellValue = cell.ToString().Trim(); // Read as text

                                                // Try to parse using a specific format (dd/MM/yyyy in this case)
                                                if (DateTime.TryParseExact(cellValue, "dd/MM/yyyy",
                                                    System.Globalization.CultureInfo.InvariantCulture,
                                                    System.Globalization.DateTimeStyles.None, out DateTime checkInDate))
                                                {
                                                    b.CheckInDate = checkInDate;
                                                }
                                                else if (double.TryParse(cellValue, out double numericDate))
                                                {
                                                    // Convert Excel serial number to DateTime
                                                    b.CheckInDate = DateTime.FromOADate(numericDate);
                                                }
                                                else
                                                {
                                                    // If parsing fails, log the error with the value for debugging
                                                    Console.WriteLine($"CheckIn Parse Failed: {cellValue}");
                                                }
                                            }
                                        }
                                    }

                                    else if (j == 3) // CheckOutDate column
                                    {
                                        if (cell != null)
                                        {
                                            if (cell.CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(cell))
                                            {
                                                // Convert numeric value (Excel serial date) to DateTime
                                                b.CheckOut = DateTime.FromOADate(cell.NumericCellValue);
                                            }
                                            else
                                            {
                                                string cellValue = cell.ToString().Trim(); // Read as text

                                                // Try to parse using a specific format (dd/MM/yyyy in this case)
                                                if (DateTime.TryParseExact(cellValue, "dd/MM/yyyy",
                                                    System.Globalization.CultureInfo.InvariantCulture,
                                                    System.Globalization.DateTimeStyles.None, out DateTime checkOutDate))
                                                {
                                                    b.CheckOut = checkOutDate;
                                                }
                                                else if (double.TryParse(cellValue, out double numericDate))
                                                {
                                                    // Convert Excel serial number to DateTime
                                                    b.CheckOut = DateTime.FromOADate(numericDate);
                                                }
                                                else
                                                {
                                                    // If parsing fails, log the error with the value for debugging
                                                    Console.WriteLine($"CheckOut Parse Failed: {cellValue}");
                                                }
                                            }
                                        }
                                    }


                                    //else if (j == 4)
                                    //    b.Requests = cell.GetFormattedCellValue();

                                    //else if (j == 13)
                                    //else if (j == 14)
                                    //else if (j == 15)
                                    //else if (j == 16)
                                    //else if (j == 17)


                                }
                            }

                            b.CreatedBy = PMS.Common.Globals.User.Email;
                            b.CreatedDate = DateTime.Now;
                            list.Add(b);

                            //if (b.PersonID != 0  && b.PriceConfigID != 0)
                            //{
                            //    list.Add(b);
                            //}

                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }

            return list;
        }

        public List<AddBedSpacePlacementVM> GetPlacementData(string filePath)
        {
            List<AddBedSpacePlacementVM> list = new List<AddBedSpacePlacementVM>();
            PMSEntities db = new PMSEntities();

            try
            {
                // Getting the complete workbook...
                var templateWorkbook = new XSSFWorkbook(filePath);

                if (templateWorkbook != null)
                {
                    // Getting the worksheet by its name...
                    //var sheet = templateWorkbook.GetSheet("Sheet1");
                    var sheet = templateWorkbook.GetSheetAt(0);

                    if (sheet != null)
                    {
                        // Getting the row... 0 is the first row.
                        //var dataRow = sheet.GetRow(4);
                        int lastRow = sheet.LastRowNum;
                        //int firstRow = sheet.FirstRowNum;
                        int firstRow = lastRow > 0 ? 1 : 0;


                        for (int i = firstRow; i <= lastRow; i++)
                        {
                            AddBedSpacePlacementVM b = new AddBedSpacePlacementVM();

                            var row = sheet.GetRow(i);
                            for (int j = row.FirstCellNum; j < row.LastCellNum; j++)
                            {
                                var cell = row.GetCell(j);
                                if (cell != null)
                                {
                                    if (j == 0)
                                    {
                                        var bookingNumber = cell.GetFormattedCellValue();
                                        var bookingID = db.Bookings.Where(x => x.BookingNumber == bookingNumber).FirstOrDefault();
                                        b.BookingID = bookingID != null ? bookingID.BookingID : 0;
                                        b.BookingNumber = bookingID != null ? bookingID.BookingNumber : bookingNumber;


                                        // Assuming mercuryID is already obtained and b is the booking view model
                                        //var mercuryID = Convert.ToInt32(cell.GetFormattedCellValue()); // MercuryID from cell

                                        //// Step 1: Get PersonID from People table by matching MercuryID
                                        //var person = db.People.Where(x => x.MercuryID == mercuryID).FirstOrDefault();
                                        //if (person != null)
                                        //{
                                        //    var personID = person.PersonID;

                                        //    // Step 2: Find bookings of the person that are not present in BedspacePlacement
                                        //    var booking = db.Bookings
                                        //                    .Where(bg => bg.PersonID == personID &&
                                        //                                !db.BedSpacePlacements.Any(bp => bp.BookingID == bg.BookingID))
                                        //                    .FirstOrDefault();

                                        //    // Step 3: Set BookingID and BookingNumber
                                        //    if (booking != null)
                                        //    {
                                        //        b.BookingID = booking.BookingID;
                                        //        b.BookingNumber = booking.BookingNumber;
                                        //    }
                                        //    else
                                        //    {
                                        //        // Handle case when no booking is found (if needed)
                                        //        b.BookingID = 0; // or any default value
                                        //        b.BookingNumber = null; // or any default value
                                        //    }
                                        //}
                                        //else
                                        //{
                                        //    // Handle case when no person is found for the given MercuryID (if needed)
                                        //    b.BookingID = 0; // or any default value
                                        //    b.BookingNumber = null; // or any default value
                                        //}


                                    }

                                    else if (j == 1)
                                    {
                                        var bedSpace = cell.GetFormattedCellValue();
                                        var bedSpaceID = db.BedSpaces.Where(x => x.BedName == bedSpace).FirstOrDefault();
                                        b.BedSpaceID = bedSpaceID != null ? bedSpaceID.BedSpaceID : 0;
                                        b.BedName = bedSpaceID != null ? bedSpaceID.BedName : bedSpace;
                                    }
                                    else if (j == 2) // MoveIn
                                    {
                                        if (cell != null)
                                        {
                                            if (cell.CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(cell))
                                            {
                                                // Convert numeric value to DateTime
                                                b.MoveIn = DateTime.FromOADate(cell.NumericCellValue);
                                            }
                                            else
                                            {
                                                string cellValue = cell.ToString().Trim(); // Read as text

                                                // Try to parse using a specific format (dd/MM/yyyy in this case)
                                                if (DateTime.TryParseExact(cellValue, "dd/MM/yyyy",
                                                    System.Globalization.CultureInfo.InvariantCulture,
                                                    System.Globalization.DateTimeStyles.None, out DateTime moveIn))
                                                {
                                                    b.MoveIn = moveIn;
                                                }
                                                else
                                                {
                                                    Console.WriteLine($"MoveIn Parse Failed: {cellValue}"); // Debugging
                                                }
                                            }
                                        }
                                    }

                                    else if (j == 3) // MoveOut
                                    {
                                        if (cell != null)
                                        {
                                            if (cell.CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(cell))
                                            {
                                                // Convert numeric value to DateTime
                                                b.MoveOut = DateTime.FromOADate(cell.NumericCellValue);
                                            }
                                            else
                                            {
                                                string cellValue = cell.ToString().Trim(); // Read as text

                                                // Try to parse using a specific format (dd/MM/yyyy in this case)
                                                if (DateTime.TryParseExact(cellValue, "dd/MM/yyyy",
                                                    System.Globalization.CultureInfo.InvariantCulture,
                                                    System.Globalization.DateTimeStyles.None, out DateTime moveOut))
                                                {
                                                    b.MoveOut = moveOut;
                                                }

                                                else
                                                {
                                                    Console.WriteLine($"MoveIn Parse Failed: {cellValue}"); // Debugging
                                                }
                                            }
                                        }
                                    }


                                    else if (j == 4) // CheckIn
                                    {
                                        if (cell != null)
                                        {
                                            DateTime? parsedDate = null;

                                            if (cell.CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(cell))
                                            {
                                                parsedDate = cell.DateCellValue;
                                            }
                                            else if (cell.CellType == CellType.String)
                                            {
                                                string cellValue = cell.ToString().Trim();

                                                // List of possible formats (single and double-digit hours)
                                                string[] formats = { "dd/MM/yyyy HH:mm", "d/M/yyyy H:mm" };

                                                if (!DateTime.TryParseExact(cellValue, formats, null, System.Globalization.DateTimeStyles.None, out DateTime checkIn))
                                                {
                                                    // Fallback to general parsing if exact format fails
                                                    DateTime.TryParse(cellValue, out checkIn);
                                                }

                                                parsedDate = checkIn;
                                            }

                                            // Assign valid date or set to null if parsing fails
                                            b.CheckIn = IsValidSqlDate(parsedDate) ? parsedDate.Value : DateTime.MinValue;
                                        }
                                    }


                                    // Helper function to validate SQL Server datetime range



                                    //else if (j == 4)
                                    //{
                                    //    b.CheckIn = ParseExcelDate(cell);

                                    //}
                                    //else if (j == 5)
                                    //{
                                    //    b.CheckOut = ParseExcelDate(cell);

                                    //}
                                    //else if (j == 5)
                                    //    b.Requests = cell.GetFormattedCellValue();
                                }
                            }

                            b.CreatedBy = PMS.Common.Globals.User.Email;
                            b.CreatedDate = DateTime.Now;
                            list.Add(b);
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }

            return list;
        }

        // Helper function to validate SQL Server datetime range
        bool IsValidSqlDate(DateTime? date)
        {
            return date.HasValue && date.Value >= new DateTime(1753, 1, 1) && date.Value <= new DateTime(9999, 12, 31);
        }


        public List<AddTermVM> GetTermData(string filePath)
        {
            List<AddTermVM> list = new List<AddTermVM>();

            try
            {
                // Load the workbook
                var templateWorkbook = new XSSFWorkbook(filePath);

                if (templateWorkbook != null)
                {
                    // Get the first sheet
                    var sheet = templateWorkbook.GetSheetAt(0);

                    if (sheet != null)
                    {
                        int lastRow = sheet.LastRowNum;
                        int firstRow = lastRow > 0 ? 1 : 0; // Skip the header row if present

                        for (int i = firstRow; i <= lastRow; i++)
                        {
                            AddTermVM m = new AddTermVM();
                            var row = sheet.GetRow(i);

                            for (int j = row.FirstCellNum; j < row.LastCellNum; j++)
                            {
                                var cell = row.GetCell(j);
                                if (cell != null)
                                {
                                    if (j == 0) // TermName
                                    {
                                        m.TermName = cell.GetFormattedCellValue();
                                    }
                                    else if (j == 1) // TermStartDate
                                    {
                                        m.TermStartDate = ParseExcelDate(cell);
                                    }
                                    else if (j == 2) // TermEndDate
                                    {
                                        m.TermEndDate = ParseExcelDate(cell);
                                    }
                                    else if (j == 3) // TermDescription
                                    {
                                        m.TermDescription = cell.GetFormattedCellValue();
                                    }
                                    else if (j == 4) // LocationId
                                    {
                                        m.LocationId = Convert.ToInt32(cell.GetFormattedCellValue());
                                    }
                                    else if (j == 5) // RateInfo
                                    {
                                        m.RateInfo = cell.GetFormattedCellValue();
                                    }
                                    else if (j == 6) // Min_Duration
                                    {
                                        m.Min_Duration = Convert.ToInt32(cell.GetFormattedCellValue());
                                    }
                                    else if (j == 7) // FrequencyId
                                    {
                                        m.FrequencyId = Convert.ToInt32(cell.GetFormattedCellValue());
                                    }
                                }
                            }

                            // Set FrequencyId if missing
                            if (m.FrequencyId == 0)
                            {
                                m.FrequencyId = (!string.IsNullOrEmpty(m.RateInfo) && m.RateInfo.Contains("Nightly")) ? 1 :
                                                 (m.RateInfo.Contains("Weekly") ? 3 :
                                                 (m.RateInfo.Contains("Monthly") ? 2 : 0));
                            }

                            m.CreatedBy = PMS.Common.Globals.User.Email;
                            m.CreatedDate = DateTime.Now;

                            if (!string.IsNullOrEmpty(m.TermName))
                            {
                                list.Add(m);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions if needed
            }

            return list;
        }

        public List<AddRoomVM> GetRoomsData(string filePath)
        {
            List<AddRoomVM> list = new List<AddRoomVM>();

            try
            {
                // Load the workbook
                var templateWorkbook = new XSSFWorkbook(filePath);

                if (templateWorkbook != null)
                {
                    // Get the first sheet
                    var sheet = templateWorkbook.GetSheetAt(0);

                    if (sheet != null)
                    {
                        int lastRow = sheet.LastRowNum;
                        int firstRow = lastRow > 0 ? 1 : 0; // Skip the header row if present

                        for (int i = firstRow; i <= lastRow; i++)
                        {
                            AddRoomVM m = new AddRoomVM();
                            var row = sheet.GetRow(i);

                            for (int j = row.FirstCellNum; j < row.LastCellNum; j++)
                            {
                                var cell = row.GetCell(j);
                                if (cell != null)
                                {
                                    if (j == 2) // TermName
                                    {
                                        m.FloorID = Convert.ToInt32(cell.GetFormattedCellValue());
                                    }

                                    else if (j == 4) // LocationId
                                    {
                                        m.RoomTypeID = Convert.ToInt32(cell.GetFormattedCellValue());
                                    }
                                    else if (j == 5) // RateInfo
                                    {
                                        m.RoomName = cell.GetFormattedCellValue();
                                    }
                                }
                            }


                            m.CreatedBy = PMS.Common.Globals.User.Email;
                            m.CreatedDate = DateTime.Now;
                            m.RoomSize = "212*122";

                            if (!string.IsNullOrEmpty(m.RoomName))
                            {
                                list.Add(m);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions if needed
            }

            return list;
        }

        public List<AddBedSpaceVM> GetBedsData(string filePath)
        {
            List<AddBedSpaceVM> list = new List<AddBedSpaceVM>();

            try
            {
                // Load the workbook
                var templateWorkbook = new XSSFWorkbook(filePath);

                if (templateWorkbook != null)
                {
                    // Get the first sheet
                    var sheet = templateWorkbook.GetSheetAt(0);

                    if (sheet != null)
                    {
                        int lastRow = sheet.LastRowNum;
                        int firstRow = lastRow > 0 ? 1 : 0; // Skip the header row if present

                        for (int i = firstRow; i <= lastRow; i++)
                        {
                            AddBedSpaceVM m = new AddBedSpaceVM();
                            var row = sheet.GetRow(i);

                            for (int j = row.FirstCellNum; j < row.LastCellNum; j++)
                            {
                                var cell = row.GetCell(j);
                                if (cell != null)
                                {
                                    if (j == 0) // TermName
                                    {
                                        m.RoomName = cell.GetFormattedCellValue();
                                    }

                                    else if (j == 1) // LocationId
                                    {
                                        m.BedSpaceName = cell.GetFormattedCellValue();
                                    }
                                    else if (j == 2) // LocationId
                                    {
                                        m.BedSpaceDescription = cell.GetFormattedCellValue();
                                    }
                                }
                            }


                            m.CreatedBy = PMS.Common.Globals.User.Email;
                            m.CreatedDate = DateTime.Now;
                            m.Status = true;
                            if (!string.IsNullOrEmpty(m.BedSpaceName))
                            {
                                list.Add(m);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions if needed
            }

            return list;
        }


        private DateTime ParseExcelDate(ICell cell)
        {
            try
            {
                if (cell != null)
                {
                    if (cell.CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(cell))
                    {
                        // For cells stored as Excel date
                        return cell.DateCellValue;
                    }
                    else if (cell.CellType == CellType.String)
                    {
                        // Attempt to parse string values manually
                        string cellValue = cell.StringCellValue.Trim();

                        // Handle specific formats if needed
                        if (DateTime.TryParseExact(
                            cellValue,
                            new[] { "dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy", "MM/dd/yyyy HH:mm:ss", "MM/dd/yyyy" },
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None,
                            out DateTime parsedDate))
                        {
                            return parsedDate;
                        }
                    }
                }
            }
            catch
            {
                // Log or handle parsing errors if necessary
            }

            // Return default value for invalid or empty cells
            return DateTime.MinValue;
        }

        public List<AddPriceConfigVM> GetPriceConfigData(string filePath)
        {
            List<AddPriceConfigVM> list = new List<AddPriceConfigVM>();

            try
            {
                // Load the workbook
                var templateWorkbook = new XSSFWorkbook(filePath);

                if (templateWorkbook != null)
                {
                    // Get the first sheet
                    var sheet = templateWorkbook.GetSheetAt(0);

                    if (sheet != null)
                    {
                        int lastRow = sheet.LastRowNum;
                        int firstRow = lastRow > 0 ? 1 : 0; // Skip the header row if present

                        for (int i = firstRow; i <= lastRow; i++)
                        {
                            AddPriceConfigVM model = new AddPriceConfigVM();
                            var row = sheet.GetRow(i);

                            for (int j = row.FirstCellNum; j < row.LastCellNum; j++)
                            {
                                var cell = row.GetCell(j);
                                if (cell != null)
                                {
                                    if (j == 0) // TermName
                                    {
                                        model.TermName = cell.GetFormattedCellValue();
                                    }
                                    else if (j == 1) // RateInfo
                                    {
                                        model.RateInfo = cell.GetFormattedCellValue();
                                    }
                                    else if (j == 2) // LocationId
                                    {
                                        model.LocationId = Convert.ToInt32(cell.GetFormattedCellValue());
                                    }
                                }
                            }
                            model.CreatedBy = PMS.Common.Globals.User.Email;
                            model.CreatedDate = DateTime.Now;

                            if (!string.IsNullOrEmpty(model.TermName))
                            {
                                list.Add(model);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions if needed
            }

            return list;
        }




        public static void ExportToExcel(HttpResponseBase Response, object data, string fileName)
        {
            var grid = new System.Web.UI.WebControls.GridView();
            grid.DataSource = data;
            grid.DataBind();
            Response.ClearContent();
            fileName = "filename=" + fileName + ".xls";
            Response.AddHeader("content-disposition", "attachment; " + fileName);
            Response.ContentType = "application/excel";
            StringWriter sw = new StringWriter();
            HtmlTextWriter htw = new HtmlTextWriter(sw);

            grid.RenderControl(htw);
            Response.Write(sw.ToString());
            Response.End();
        }
    }
    public static class Extension
    {
        public static List<T> ToList<T>(this DataTable dt)
        {
            List<T> data = new List<T>();
            foreach (DataRow row in dt.Rows)
            {
                T item = GetItem<T>(row);
                data.Add(item);
            }
            return data;
        }

        private static T GetItem<T>(DataRow dr)
        {
            Type temp = typeof(T);
            T obj = Activator.CreateInstance<T>();

            foreach (DataColumn column in dr.Table.Columns)
            {
                foreach (PropertyInfo pro in temp.GetProperties())
                {
                    if (pro.Name == column.ColumnName)
                        pro.SetValue(obj, dr[column.ColumnName], null);
                    else
                        continue;
                }
            }
            return obj;
        }

        public static string GetFormattedCellValue(this ICell cell, IFormulaEvaluator eval = null)
        {
            if (cell != null)
            {
                switch (cell.CellType)
                {
                    case CellType.String:
                        return cell.StringCellValue;

                    case CellType.Numeric:
                        if (DateUtil.IsCellDateFormatted(cell))
                        {
                            DateTime date = cell.DateCellValue;
                            ICellStyle style = cell.CellStyle;
                            // Excel uses lowercase m for month whereas .Net uses uppercase
                            string format = style.GetDataFormatString().Replace('m', 'M');
                            return date.ToString(format);
                        }
                        else
                        {
                            return cell.NumericCellValue.ToString();
                        }

                    case CellType.Boolean:
                        return cell.BooleanCellValue ? "TRUE" : "FALSE";

                    case CellType.Formula:
                        if (eval != null)
                            return GetFormattedCellValue(eval.EvaluateInCell(cell));
                        else
                            return cell.CellFormula;

                    case CellType.Error:
                        return FormulaError.ForInt(cell.ErrorCellValue).String;
                }
            }
            // null or blank cell, or unknown cell type
            return string.Empty;
        }
    }
}