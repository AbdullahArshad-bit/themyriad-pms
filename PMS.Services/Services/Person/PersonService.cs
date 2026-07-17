using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.DTO.ViewModels.PersonManageViewModels;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using PMS.Common.Classes;
using PMS.Services.Services.AuditLogs;
using PMS.DTO.ViewModels.UserManageViewModels;
using System.Web.Security;
using PMS.DTO.ViewModels.CorrespondenceViewModels;
using PMS.Services.Services.UserManage;
using PMS.Services.Services.Email;
using PMS.Services.Services.Correspondence;
using System.Configuration;
using System.Web;
using System.Data.Entity;
using PMS.DTO.ViewModels.BedSpacePlacementViewModels;
using System.Data.Entity.Core.Objects;
using PMS.DTO.ViewModels.ApiViewModels;
using PMS.DTO.ViewModels;
using PMS.DTO.ViewModels.NetIntViewModel;
using Ninject.Activation;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Data;
using PMS.Services.Services.LocationContext;
using PMS.Common;

namespace PMS.Services.Services.Person
{
    public class PersonService : IPersonService
    {
        private readonly UnitOfWork<PMSEntities> uow;
        private readonly IAuditLogsService auditLogsService;
        private readonly IUserManageService userService;
        private readonly IEmailService emailService;
        private readonly ICorrespondenceService correspondenceService;
        private readonly ILocationContextService locationContextService;
        private static readonly object codeLock = new object();
        public PersonService(UnitOfWork<PMSEntities> _uow, IAuditLogsService _auditLogsService, IUserManageService _userService, IEmailService _emailService, ICorrespondenceService _correspondenceService, ILocationContextService _locationContextService)
        {
            uow = _uow;
            auditLogsService = _auditLogsService;
            userService = _userService;
            emailService = _emailService;
            correspondenceService = _correspondenceService;
            locationContextService = _locationContextService;
        }

        //public List<EF.Person> GetPersons()
        //{
        //    return uow.GenericRepository<EF.Person>().Table.Where(x => x.IsEnable == true).ToList();
        //}

        public List<EF.Person> GetPersons()
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();
            var t = uow.GenericRepository<EF.Person>().Table.Where(x => x.IsEnable == true && assignedLocationIds.Contains((int)x.LocationId)).ToList();
            return t;
        }

        public PersonResponse GetPersonsByPages(PersonBinding person, string searchValue, string start, string lenght, string QueryBY, string orderBy, string orderDir, List<string> selectedColumns)
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            IQueryable<EF.Person> data = Enumerable.Empty<EF.Person>().AsQueryable();
            int TotalRecords = data.Count();

            data = uow.GenericRepository<EF.Person>().Table.Where(x => x.IsEnable == true && assignedLocationIds.Contains((int)x.LocationId));

            List<string> selectedColumn = person.SelectedColumns ?? new List<string>();

            List<string> allColumns = new List<string>
    {
        "PersonID", "Location", "MyriadID", "ResidentID", "ReferralCode",
        "Title", "FullName", "Gender", "Email", "University", "Phone","DOB", "VehicleNumber","MercuryID","PassportNumber","ProfileNotes","UniversityStudentID"
    };
            List<string> unselectedColumns = allColumns.Except(selectedColumn).ToList();
            //For Server Side Sorting

            if (!string.IsNullOrEmpty(person.search.value) && !string.IsNullOrEmpty(person.search.column) && person.query == null)
            {
                person.search.value = person.search.value.ToLower();
                switch (person.search.column.ToLower())
                {
                    case "locationname":
                        data = data.Where(x => x.Location.LocationName.ToLower().Contains(person.search.value));
                        break;
                    case "myriadid":
                        data = data.Where(x => x.Code.ToLower().Contains(person.search.value));
                        break;
                    case "residentid":
                        data = data.Where(x => x.ResidentID.ToLower().Contains(person.search.value));
                        break;
                    case "referralcode":
                        data = data.Where(x => x.ReferralCode.ToLower().Contains(person.search.value));
                        break;
                    case "title":
                        data = data.Where(x => x.Title.ToLower().Contains(person.search.value));
                        break;
                    case "fullname":
                        data = data.Where(x => x.FullName.ToLower().Contains(person.search.value));
                        break;
                    case "gender":
                        data = data.Where(x => x.Gender.ToLower().Contains(person.search.value));
                        break;
                    case "email":
                        data = data.Where(x => x.Email.ToLower().Contains(person.search.value));
                        break;
                    case "university":
                        data = data.Where(x => x.University.UniversityName.ToLower().Contains(person.search.value));
                        break;
                    case "phone":
                        data = data.Where(x => x.Phone.ToLower().Contains(person.search.value));
                        break;
                    case "vehiclenumber":
                        data = data.Where(x => x.VehicleNumber.ToLower().Contains(person.search.value));
                        break;
                    case "mercuryid":
                        data = data.Where(x => x.MercuryID.ToLower().Contains(person.search.value));
                        break;
                    case "passportnumber":
                        data = data.Where(x => x.PassportNumber.ToLower().Contains(person.search.value));
                        break;
                    case "profilenotes":
                        data = data.Where(x => x.ProfileNotes.ToLower().Contains(person.search.value));
                        break;
                    case "universitystudentid":
                        data = data.Where(x => x.UniversityStudentID.ToLower().Contains(person.search.value));
                        break;

                    // Add more cases for other columns if needed
                    default:
                        data = data.OrderByDescending(x => x.PersonID);
                        break;
                }
            }
            else if (!string.IsNullOrEmpty(searchValue))
            {
                data = data.Where(x =>
         x.Code.Contains(searchValue) ||
         x.ResidentID.Contains(searchValue) ||
         x.ReferralCode.Contains(searchValue) ||
         x.Title.Contains(searchValue) ||
         x.FullName.Contains(searchValue) ||
         x.Gender.Contains(searchValue) ||
         x.Email.Contains(searchValue) ||
         x.University.UniversityName.Contains(searchValue) ||
         x.Phone.Contains(searchValue) ||
         x.VehicleNumber.Contains(searchValue) ||
         x.MercuryID.Contains(searchValue) ||
         x.PassportNumber.Contains(searchValue) ||
         x.ProfileNotes.Contains(searchValue) ||
         x.UniversityStudentID.Contains(searchValue)
         );


            }
            else
            {
                data = data.OrderByDescending(x => x.PersonID);
            }

            switch (person.query)
            {
                case "Notcreatedbooking":
                    data = data.Where(x => x.Bookings.Count() == 0);
                    break;
            }
            if (!string.IsNullOrEmpty(orderBy))
            {
                switch (orderBy)
                {
                    case "PersonID":
                        data = orderDir == "asc" ? data.OrderBy(x => x.PersonID) : data.OrderByDescending(x => x.PersonID);
                        break;
                    case "Location":
                        data = orderDir == "asc" ? data.OrderBy(x => x.LocationId) : data.OrderByDescending(x => x.LocationId);
                        break;
                    case "MyriadID":
                        data = orderDir == "asc" ? data.OrderBy(x => x.CreatedDate) : data.OrderByDescending(x => x.CreatedDate);
                        break;
                    case "ResidentID":
                        data = orderDir == "asc" ? data.OrderBy(x => x.ResidentID) : data.OrderByDescending(x => x.ResidentID);
                        break;
                    case "ReferralCode":
                        data = orderDir == "asc" ? data.OrderBy(x => x.ReferralCode) : data.OrderByDescending(x => x.ReferralCode);
                        break;
                    case "Title":
                        data = orderDir == "asc" ? data.OrderBy(x => x.Title) : data.OrderByDescending(x => x.Title);
                        break;
                    case "FullName":
                        data = orderDir == "asc" ? data.OrderBy(x => x.FullName) : data.OrderByDescending(x => x.FullName);
                        break;
                    case "Gender":
                        data = orderDir == "asc" ? data.OrderBy(x => x.Gender) : data.OrderByDescending(x => x.Gender);
                        break;
                    case "Email":
                        data = orderDir == "asc" ? data.OrderBy(x => x.Email) : data.OrderByDescending(x => x.Email);
                        break;
                    case "University":
                        data = orderDir == "asc" ? data.OrderBy(x => x.University.UniversityName) : data.OrderByDescending(x => x.University.UniversityName);
                        break;
                    case "Phone":
                        data = orderDir == "asc" ? data.OrderBy(x => x.Phone) : data.OrderByDescending(x => x.Phone);
                        break;
                    case "DOB":
                        data = orderDir == "asc" ? data.OrderBy(x => x.DOB) : data.OrderByDescending(x => x.DOB);
                        break;
                    case "VehicleNumber":
                        data = orderDir == "asc" ? data.OrderBy(x => x.VehicleNumber) : data.OrderByDescending(x => x.VehicleNumber);
                        break;
                    case "MercuryID":
                        data = orderDir == "asc" ? data.OrderBy(x => x.MercuryID) : data.OrderByDescending(x => x.MercuryID);
                        break;
                    case "PassportNumber":
                        data = orderDir == "asc" ? data.OrderBy(x => x.PassportNumber) : data.OrderByDescending(x => x.PassportNumber);
                        break;
                    case "ProfileNotes":
                        data = orderDir == "asc" ? data.OrderBy(x => x.ProfileNotes) : data.OrderByDescending(x => x.ProfileNotes);
                        break;
                    case "UniversityStudentID":
                        data = orderDir == "asc" ? data.OrderBy(x => x.UniversityStudentID) : data.OrderByDescending(x => x.UniversityStudentID);
                        break;
                    default:
                        data = data.OrderByDescending(x => x.PersonID);


                        break;

                }
            }
            else
            {
                // Default sorting if orderBy is null or empty
                data = data.OrderByDescending(x => x.PersonID);
            }
            var List = data.Skip(int.Parse(person.start)).Take(int.Parse(person.length)).Select(x => new PersonViewModels
            {
                PersonID = x.PersonID,
                Location = unselectedColumns.Contains("Location") ? x.Location.LocationName : null,
                MyriadID = unselectedColumns.Contains("MyriadID") ? x.Code : null,
                ResidentID = unselectedColumns.Contains("ResidentID") ? x.ResidentID : null,
                ReferralCode = unselectedColumns.Contains("ReferralCode") ? x.ReferralCode : null,
                Title = unselectedColumns.Contains("Title") ? x.Title : null,
                FullName = unselectedColumns.Contains("FullName") ? x.FullName : null,
                Gender = unselectedColumns.Contains("Gender") ? x.Gender : null,
                Email = unselectedColumns.Contains("Email") ? x.Email : null,
                University = unselectedColumns.Contains("University") ? x.University.UniversityName : null,
                Phone = unselectedColumns.Contains("Phone") ? x.Phone : null,
                DOB = unselectedColumns.Contains("DOB") ? x.DOB : (DateTime?)null,
                VehicleNumber = unselectedColumns.Contains("VehicleNumber") ? x.VehicleNumber : null,
                MercuryID = unselectedColumns.Contains("MercuryID") ? x.MercuryID : null,
                PassportNumber = unselectedColumns.Contains("PassportNumber") ? x.PassportNumber : null,
                ProfileNotes = unselectedColumns.Contains("ProfileNotes") ? x.ProfileNotes : null,
                UniversityStudentID = unselectedColumns.Contains("UniversityStudentID") ? x.UniversityStudentID : null
            }).ToList();

            var result = new PersonResponse();
            if (QueryBY != null && QueryBY != "" && person.query == "Notcreatedbooking")
            {
                result.person.ToList();
            }

            else
            {
                result.person = List;
                result.RecordsFiltered = data.Count();
                result.TotalRecords = data.Count();
                // uow.Context.Configuration.LazyLoadingEnabled = false;
            }
            return result;
        }
        public List<EF.Person> GetPersonsExport()
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();


            var data = uow.GenericRepository<EF.Person>().Table.Where(x => x.IsEnable == true && assignedLocationIds.Contains((int)x.LocationId)).OrderByDescending(x => x.CreatedDate).ToList();

            return data;
        }


        public List<EF.Person> GetCheckInPersons()
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            var data = (from person in uow.GenericRepository<EF.Person>().Table
                        join booking in uow.GenericRepository<EF.Booking>().Table on person.PersonID equals booking.PersonID
                        join bedSpacePlacement in uow.GenericRepository<EF.BedSpacePlacement>().Table on booking.BookingID equals bedSpacePlacement.BookingID
                        where person.IsEnable && assignedLocationIds.Contains((int)person.LocationId)
                              && bedSpacePlacement.CheckIn != null && bedSpacePlacement.CheckOut == null && bedSpacePlacement.GuestCount == null &&
                              booking.IsEnable == true && bedSpacePlacement.IsEnable == true
                        select person).ToList();

            return data;
        }

        public List<EF.V_GetPersonsforDeposit> GetPersonsNotCheckedinYet()
        {
            var data = uow.GenericRepository<V_GetPersonsforDeposit>().Table.ToList();

            return data;
        }

        public List<EF.V_GetPersonsforDeposit> GetPersonsNotCheckedinYet(int invoiceTypeId)
        {
            if (invoiceTypeId == (int)Enumeration.InvoiceTypes.Miscellaneous) // Miscellaneous
            {
                var data = uow.GenericRepository<V_GetPersonsforMiscellaneous>().Table.ToList();
                return data.Select(x => new V_GetPersonsforDeposit
                {
                    PersonID = x.PersonID,
                    FullName = x.FullName,
                    Email = x.Email,
                    Code = x.Code,
                    LocationId = x.LocationId
                }).ToList();
            }
            else // Deposit
            {
                var data = uow.GenericRepository<V_GetPersonsforDeposit>().Table.ToList();
                return data;
            }
        }

        public List<EF.Person> GetPersonsReservedCurrently()
        {
            var data = (from person in uow.Context.People

                        join b in uow.Context.Bookings
                        on person.PersonID equals b.PersonID
                        into per
                        from booking in per.DefaultIfEmpty()

                        join p in uow.Context.BedSpacePlacements
                        on booking.BookingID equals p.BookingID
                        into bed
                        from p in bed.Where(x => x.IsEnable == true).DefaultIfEmpty()

                        where person.IsEnable == true && booking.IsEnable == true && bed.Any(x => x.CheckIn != null && x.CheckOut == null && x.IsEnable == true)

                        select person);

            return data.ToList();
        }
        public List<EF.Person> GetPersonsReservedCurrentlyOrCheckedOut()
        {
            var data = (from person in uow.Context.People

                        join b in uow.Context.Bookings
                        on person.PersonID equals b.PersonID
                        into per
                        from booking in per.DefaultIfEmpty()

                        join p in uow.Context.BedSpacePlacements
                        on booking.BookingID equals p.BookingID
                        into bed
                        from p in bed.Where(x => x.IsEnable == true).DefaultIfEmpty()

                        where person.IsEnable == true && booking.IsEnable == true && bed.Any(x => x.CheckIn != null && x.IsEnable == true || x.CheckOut != null && x.IsEnable == true)

                        select person);

            return data.ToList();
        }

        public EF.Person GetPersonById(int id)
        {
            return uow.GenericRepository<EF.Person>().GetById(id);
        }

        public EF.Person AddImportPerson(AddPersonVM personVM, HttpPostedFileBase file)
        {
            var university = uow.GenericRepository<EF.University>().Table
                              .FirstOrDefault(u => u.UniversityName == personVM.UniversityName);
            if (personVM.UniversityId == 0)
            {
                personVM.UniversityId = university?.Id ?? 1;
            }
            bool existWithName = uow.GenericRepository<EF.Person>().Table.Any(x =>
                x.IsEnable == true && x.Email == personVM.Email && x.FullName == personVM.FullName &&
                x.LocationId == personVM.LocationId);
            if (existWithName)
                throw new Exception("Resident already exist.");

            EF.Person person = new EF.Person
            {
                Title = personVM.Title,
                FullName = personVM.FullName,
                Email = personVM.Email,
                Phone = personVM.Phone ?? "0",
                Gender = personVM.Gender,
                DOB = personVM.DOB,
                Nationality = personVM.Nationality,
                CreatedDate = personVM.CreatedDate,
                CreatedBy = personVM.CreatedBy,
                IsEnable = true,
                LocationId = personVM.LocationId,
                UniversityId = personVM.UniversityId,
                ResidentID = personVM.ResidentID,
                ReferralCode = personVM.ReferralCode,
                VehicleNumber = personVM.VehicleNumber,
                ImageUrl = personVM.ImageUrl,
                PassportNumber = personVM.PassportNumber,
                Religion = personVM.Religion,
                CampusEmail = personVM.CampusEmail,
                WhatsappNumber = personVM.ResidentWhatsappNumber,
                MercuryID = personVM.MercuryID,
                ProfileNotes = personVM.ProfileNotes,
                UniversityStudentID = personVM.UniversityStudentID
            };
            // Robust insert with SERIALIZABLE to avoid duplicate codes during import
            var saved = false;
            int retries = 0;
            while (!saved && retries < 3)
            {
                using (var tx = uow.Context.Database.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        person.Code = GetNextPersonCodeWithLock(personVM.LocationId);
                        uow.GenericRepository<EF.Person>().Insert(person);
                        uow.SaveChanges();
                        tx.Commit();
                        saved = true;
                    }
                    catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
                    {
                        tx.Rollback();
                        retries++;
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }

            EmergencyContact emergencyContact = new EmergencyContact
            {
                PersonID = person.PersonID,
                FullName = personVM.GuardianFullName,
                Phone = personVM.GuardianPhone,
                PassportNumber = personVM.GuardianPassportNumber,
                Email = personVM.GuardianEmail,
                Relation = personVM.GuardianRelation
            };

            uow.GenericRepository<EmergencyContact>().Insert(emergencyContact);
            uow.SaveChanges();

            return person;
        }

        public EF.Person AddPerson(AddPersonVM personVM, HttpPostedFileBase file)
        {
            bool residentIdNumberExist = uow.GenericRepository<EF.Person>().Table.Any(x => x.IsEnable == true && (x.ResidentID == personVM.ResidentID && personVM.ResidentID != null) && x.LocationId == personVM.LocationId);
            if (residentIdNumberExist)
                throw new Exception("ResidentID Number already registered.");
            if (file != null)
            {
                var result = Common.ImageUpload.SaveFile(file, "ProfilePhotos");
                personVM.ImageUrl = "/Upload/Files/ProfilePhotos/" + result;
            }
            EF.Person person = new EF.Person
            {
                Title = personVM.Title,
                FullName = personVM.FullName,
                Email = personVM.Email,
                SecondaryEmail = personVM.SecondaryEmail,
                Phone = personVM.Phone,
                Gender = personVM.Gender,
                DOB = personVM.DOB,
                Nationality = personVM.Nationality,
                CreatedDate = personVM.CreatedDate,
                CreatedBy = personVM.CreatedBy,
                IsEnable = true,
                LocationId = personVM.LocationId,
                UniversityId = personVM.UniversityId,
                ResidentID = personVM.ResidentID,
                ReferralCode = personVM.ReferralCode,
                VehicleNumber = personVM.VehicleNumber,
                ImageUrl = personVM.ImageUrl,
                PassportNumber = personVM.PassportNumber,
                Religion = personVM.Religion,
                CampusEmail = personVM.CampusEmail,
                WhatsappNumber = personVM.ResidentWhatsappNumber,
                MercuryID = personVM.MercuryID,
                ProfileNotes = personVM.ProfileNotes,
                UniversityStudentID = personVM.UniversityStudentID
            };

            bool saved = false;
            int retryCount = 0;
            while (!saved && retryCount < 3)
            {
                using (var tx = uow.Context.Database.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        person.Code = GetNextPersonCodeWithLock(personVM.LocationId);
                        uow.GenericRepository<EF.Person>().Insert(person);
                        uow.SaveChanges();
                        tx.Commit();
                        saved = true;
                    }
                    catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
                    {
                        tx.Rollback();
                        retryCount++;
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }

            if (!saved)
                throw new Exception("Unable to save person due to duplicate code generation.");
            if (!string.IsNullOrEmpty(personVM.GuardianFullName) && !string.IsNullOrEmpty(personVM.GuardianPhone))
            {
                EmergencyContact emergencyContact = new EmergencyContact
                {
                    PersonID = person.PersonID,
                    FullName = personVM.GuardianFullName,
                    Phone = personVM.GuardianPhone,
                    Email = personVM.GuardianOtherEmail,
                    Relation = personVM.GuardianRelation,
                    PassportNumber = personVM.GuardianPassportNumber
                };

                uow.GenericRepository<EmergencyContact>().Insert(emergencyContact);
                uow.SaveChanges();
            }

            var oldperson = new EF.Person();

            //Insert Audit Log
            {
                var difference = Common.Classes.Common.DetailedCompare<EF.Person>(oldperson, person);
                List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                EF.AuditLog auditLog = new EF.AuditLog()
                {
                    AuditType = (int)Enumeration.AuditType.Create,
                    ActionId = (int)Enumeration.CorrespondenceAction.CreatePerson,
                    PK = person.PersonID.ToString(),
                    UserId = Common.Globals.User.ID,
                    TableName = "Person",
                    Reference = person.Code.ToString(),
                    UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                    PersonId = person.PersonID,
                    AuditLogDetails = difference
                };
                auditLogsService.AddAuditLog(auditLog);
            }
            return person;
        }

        private bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            var sqlException = ex.InnerException?.InnerException as SqlException;
            return sqlException != null && (sqlException.Number == 2627 || sqlException.Number == 2601);
        }

        public EF.Person AddPersonWithoutSaving(AddPersonVM personVM, HttpPostedFileBase file)
        {
            var university = uow.GenericRepository<EF.University>().Table
                                .FirstOrDefault(u => u.UniversityName == personVM.UniversityName);
            if (personVM.UniversityId == 0)
            {
                personVM.UniversityId = university?.Id ?? 0;
            }

            bool existWithName = uow.GenericRepository<EF.Person>().Table.Any(x =>
                x.IsEnable == true && x.Email == personVM.Email && x.FullName == personVM.FullName &&
                x.LocationId == personVM.LocationId && x.DOB == personVM.DOB);

            if (existWithName)
            {
                Console.WriteLine($"Skipping duplicate person: {personVM.FullName}");
                return new EF.Person(); // Return an empty object instead of throwing an exception
            }

            if (file != null)
            {
                var result = Common.ImageUpload.SaveFile(file, "ProfilePhotos");
                personVM.ImageUrl = "/Upload/Files/ProfilePhotos/" + result;
            }

            EF.Person person = new EF.Person
            {
                Title = personVM.Title,
                FullName = personVM.FullName,
                Email = personVM.Email,
                Phone = personVM.Phone ?? "0",
                Gender = personVM.Gender,
                DOB = personVM.DOB,
                Nationality = personVM.Nationality,
                CreatedDate = personVM.CreatedDate,
                CreatedBy = personVM.CreatedBy,
                IsEnable = true,
                LocationId = personVM.LocationId,
                UniversityId = personVM.UniversityId,
                MercuryID = personVM.MercuryID
            };

            person.Code = GetMaxPersonCode(personVM.LocationId);

            // Save the person first so PersonID is assigned
            uow.GenericRepository<EF.Person>().Insert(person);
            uow.SaveChanges(); // Ensures PersonID is available

            // Handle emergency contact separately to avoid failure affecting person insert
            if (!string.IsNullOrEmpty(personVM.GuardianFullName) || !string.IsNullOrEmpty(personVM.GuardianPhone))
            {
                try
                {
                    EmergencyContact emergencyContact = new EmergencyContact
                    {
                        PersonID = person.PersonID, // Now PersonID exists
                        FullName = personVM.GuardianFullName,
                        Phone = personVM.GuardianPhone,
                        Email = "Not Specified",
                        Relation = "Not Specified",
                    };

                    uow.GenericRepository<EmergencyContact>().Insert(emergencyContact);
                    uow.SaveChanges(); // Save emergency contact separately
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to add emergency contact for {personVM.FullName}: {ex.Message}");
                }
            }

            return person;
        }

        public EF.Person AddPersonWithBooking(AddPersonVM personVM)
        {
            bool exist = uow.GenericRepository<EF.Person>().Table.Any(x => x.IsEnable == true && x.Email == personVM.Email);
            if (exist)
                throw new Exception("Email already registered.");

            EF.Person person = new EF.Person
            {
                Title = personVM.Title,
                FullName = personVM.FullName,
                Email = personVM.Email,
                Phone = personVM.Phone,
                Gender = personVM.Gender,
                DOB = personVM.DOB,
                Nationality = personVM.Nationality,
                CreatedDate = personVM.CreatedDate,
                CreatedBy = personVM.CreatedBy,
                IsEnable = true,
                LocationId = personVM.LocationId,
                UniversityId = personVM.UniversityId
            };
            EF.Booking booking = null;
            using (var tx = uow.Context.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                person.Code = GetNextPersonCodeWithLock(personVM.LocationId);
                booking = new EF.Booking()
                {
                    BookingNumber = Common.Globals.GetBookingNumber((int)person.LocationId),
                    LocationID = person.LocationId,
                    PriceConfigID = personVM.Booking.PriceConfigID,
                    CheckInDate = personVM.Booking.CheckInDate,
                    CheckOutDate = personVM.Booking.CheckOut,
                    AccessibilityRequest = personVM.Booking.Requests,
                    CreatedBy = person.CreatedBy,
                    CreatedDate = person.CreatedDate,
                    IsCancel = false,
                    IsEnable = true,
                    Channel = "Bulk import",
                };
                person.Bookings.Add(booking);
                uow.GenericRepository<EF.Person>().Insert(person);
                uow.SaveChanges();
                tx.Commit();
            }


            var oldperson = new EF.Person();
            var Oldbooking = new EF.Booking();
            List<EF.AuditLog> auditLogList = new List<EF.AuditLog>();


            //Insert Audit Log add to list Person
            {
                var difference = Common.Classes.Common.DetailedCompare<EF.Person>(oldperson, person);
                List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                EF.AuditLog auditLog = new EF.AuditLog()
                {
                    AuditType = (int)Enumeration.AuditType.Create,
                    ActionId = (int)Enumeration.CorrespondenceAction.CreatePerson,
                    PK = person.PersonID.ToString(),
                    UserId = 1,
                    TableName = "Person",
                    Reference = person.Code.ToString() + " from " + booking.Channel,
                    UserName = person.Email,
                    PersonId = person.PersonID,
                    TimeStamp = DateTime.Now,
                    AuditLogDetails = difference
                };

                auditLogList.Add(auditLog);
            }

            //Insert Audit Log for booking add to list
            {
                var difference = Common.Classes.Common.DetailedCompare<EF.Booking>(Oldbooking, booking);
                List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                EF.AuditLog auditLog = new EF.AuditLog()
                {
                    AuditType = (int)Enumeration.AuditType.Create,
                    ActionId = (int)Enumeration.CorrespondenceAction.CreateBooking,
                    PK = booking.BookingID.ToString(),
                    UserId = 1,
                    TableName = "Booking",
                    Reference = booking.BookingNumber + " from " + booking.Channel,
                    UserName = person.Email,
                    PersonId = booking.PersonID,
                    TimeStamp = DateTime.Now,
                    AuditLogDetails = difference
                };

                auditLogList.Add(auditLog);
            }
            auditLogsService.AddAuditLogList(auditLogList);
            return person;
        }

        public async Task<ImportPersonsWithBookingResultVM> ImportPersonsWithBookingAsync(List<AddPersonVM> personVMs)
        {
            var result = new ImportPersonsWithBookingResultVM();
            if (personVMs == null || !personVMs.Any())
                return result;

            var importLocationId = personVMs
                .Select(x => x.LocationId)
                .FirstOrDefault(x => x > 0);

            if (importLocationId <= 0)
            {
                foreach (var personVM in personVMs)
                    SetImportError(result, personVM, "Location not found.");

                return result;
            }

            var importLocation = await uow.Context.Locations
                .FirstOrDefaultAsync(x => x.IsEnable && x.LocationID == importLocationId);

            if (importLocation == null)
            {
                foreach (var personVM in personVMs)
                    SetImportError(result, personVM, "Location not found.");

                return result;
            }

            var universities = await uow.Context.Universities
                .Where(x => x.IsEnable && (x.LocationId ?? 0) == importLocationId)
                .ToListAsync();
            var universityLookup = universities
                .GroupBy(x => NormalizeImportValue(x.UniversityName))
                .ToDictionary(x => x.Key, x => x.First());

            var priceConfigs = await uow.Context.PriceConfigs
                .Include(x => x.Term)
                .Include(x => x.RoomType)
                .Where(x => x.IsEnable && (x.LocationId ?? 0) == importLocationId)
                .ToListAsync();
            var priceConfigLookup = BuildPriceConfigLookup(priceConfigs);

            var importEmails = personVMs
                .Select(x => NormalizeImportValue(x.Email))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            var existingEmails = new HashSet<string>(
                await uow.Context.People
                    .Where(x => x.IsEnable && importEmails.Contains(x.Email.ToLower()))
                    .Select(x => x.Email.ToLower())
                    .ToListAsync());

            var fileEmails = new HashSet<string>();
            var validRows = new List<Tuple<AddPersonVM, EF.Person, EF.Booking>>();

            foreach (var personVM in personVMs)
            {
                personVM.ImportError = null;

                var locationId = importLocationId;

                personVM.Title = string.IsNullOrWhiteSpace(personVM.Title) ? GetDefaultTitle(personVM.Gender) : personVM.Title.Trim();
                personVM.FullName = personVM.FullName?.Trim();
                personVM.Email = personVM.Email?.Trim();
                personVM.Phone = personVM.Phone?.Trim();
                personVM.PassportNumber = personVM.PassportNumber?.Trim();
                personVM.Gender = personVM.Gender?.Trim();
                personVM.Nationality = personVM.Nationality?.Trim();
                personVM.GuardianFullName = personVM.GuardianFullName?.Trim();
                personVM.GuardianPhone = personVM.GuardianPhone?.Trim();
                personVM.GuardianRelation = personVM.GuardianRelation?.Trim();
                personVM.UniversityName = personVM.UniversityName?.Trim();
                personVM.GuardianOtherEmail = personVM.GuardianOtherEmail?.Trim();
                personVM.GuardianEmail = personVM.GuardianEmail?.Trim();

                if (string.IsNullOrWhiteSpace(personVM.FullName) ||
                    string.IsNullOrWhiteSpace(personVM.Email) ||
                    string.IsNullOrWhiteSpace(personVM.Phone) ||
                    string.IsNullOrWhiteSpace(personVM.PassportNumber) ||
                    string.IsNullOrWhiteSpace(personVM.Gender) ||
                    personVM.DOB == default(DateTime) ||
                    string.IsNullOrWhiteSpace(personVM.Nationality) ||
                    string.IsNullOrWhiteSpace(personVM.UniversityName) ||
                    string.IsNullOrWhiteSpace(personVM.GuardianFullName) ||
                    string.IsNullOrWhiteSpace(personVM.GuardianPhone) ||
                    string.IsNullOrWhiteSpace(personVM.GuardianRelation))
                {
                    SetImportError(result, personVM, "Required person fields are missing.");
                    continue;
                }

                if (personVM.Booking == null ||
                    string.IsNullOrWhiteSpace(personVM.Booking.TermName) ||
                    string.IsNullOrWhiteSpace(personVM.Booking.RoomTypeName) ||
                    !personVM.Booking.ImportPrice.HasValue ||
                    personVM.Booking.CheckInDate == default(DateTime) ||
                    !personVM.Booking.CheckOut.HasValue)
                {
                    SetImportError(result, personVM, "Required booking fields are missing.");
                    continue;
                }

                var normalizedEmail = NormalizeImportValue(personVM.Email);
                if (existingEmails.Contains(normalizedEmail))
                {
                    SetImportError(result, personVM, "Email already registered.");
                    continue;
                }

                if (!fileEmails.Add(normalizedEmail))
                {
                    SetImportError(result, personVM, "Duplicate email found in import file.");
                    continue;
                }

                var university = ResolveUniversity(universityLookup, personVM.UniversityName);
                if (university == null)
                {
                    SetImportError(result, personVM, "University not found.");
                    continue;
                }

                var priceConfig = ResolvePriceConfig(priceConfigLookup, personVM.Booking);
                if (priceConfig == null)
                {
                    SetImportError(result, personVM, "Price configuration not found for term, room type, and price.");
                    continue;
                }

                personVM.UniversityId = university.Id;
                personVM.Booking.PriceConfigID = priceConfig.PriceConfigID;

                var person = new EF.Person
                {
                    Title = personVM.Title,
                    FullName = personVM.FullName,
                    Email = personVM.Email,
                    Phone = personVM.Phone,
                    Gender = personVM.Gender,
                    DOB = personVM.DOB,
                    Nationality = personVM.Nationality,
                    CreatedDate = personVM.CreatedDate,
                    CreatedBy = personVM.CreatedBy,
                    IsEnable = true,
                    LocationId = locationId,
                    UniversityId = university.Id,
                    PassportNumber = personVM.PassportNumber
                };

                person.EmergencyContacts.Add(new EmergencyContact
                {
                    FullName = personVM.GuardianFullName,
                    Phone = personVM.GuardianPhone,
                    Email = personVM.GuardianOtherEmail,
                    Relation = personVM.GuardianRelation
                });

                var booking = new EF.Booking
                {
                    BookingNumber = Common.Globals.GetBookingNumber(locationId),
                    LocationID = locationId,
                    PriceConfigID = priceConfig.PriceConfigID,
                    CheckInDate = personVM.Booking.CheckInDate,
                    CheckOutDate = personVM.Booking.CheckOut,
                    AccessibilityRequest = personVM.Booking.Requests,
                    CreatedBy = personVM.CreatedBy,
                    CreatedDate = personVM.CreatedDate,
                    IsCancel = false,
                    IsEnable = true,
                    Status = false,
                    Channel = "Bulk import"
                };

                person.Bookings.Add(booking);
                validRows.Add(Tuple.Create(personVM, person, booking));
            }

            if (!validRows.Any())
                return result;

            using (var tx = uow.Context.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    var nextCode = GetNextPersonSequenceWithLock(importLocationId);

                    foreach (var row in validRows)
                    {
                        var person = row.Item2;
                        person.Code = FormatPersonCode(importLocation.Prefix, nextCode);
                        nextCode++;
                    }

                    uow.Context.People.AddRange(validRows.Select(x => x.Item2));
                    await uow.Context.SaveChangesAsync();
                    tx.Commit();
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    foreach (var row in validRows)
                    {
                        SetImportError(result, row.Item1, "Unable to save import row. " + ex.Message);
                    }
                    return result;
                }
            }

            var auditLogList = new List<EF.AuditLog>();
            foreach (var row in validRows)
            {
                var personVM = row.Item1;
                var person = row.Item2;
                var booking = row.Item3;

                result.SavedPersons.Add(personVM);

                auditLogList.Add(new EF.AuditLog
                {
                    AuditType = (int)Enumeration.AuditType.Create,
                    ActionId = (int)Enumeration.CorrespondenceAction.CreatePerson,
                    PK = person.PersonID.ToString(),
                    UserId = Common.Globals.User.ID,
                    TableName = "Person",
                    Reference = person.Code,
                    UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                    PersonId = person.PersonID,
                    TimeStamp = DateTime.Now,
                    AuditLogDetails = Common.Classes.Common.DetailedCompare<EF.Person>(new EF.Person(), person)
                });

                auditLogList.Add(new EF.AuditLog
                {
                    AuditType = (int)Enumeration.AuditType.Create,
                    ActionId = (int)Enumeration.CorrespondenceAction.CreateBooking,
                    PK = booking.BookingID.ToString(),
                    UserId = Common.Globals.User.ID,
                    TableName = "Booking",
                    Reference = booking.BookingNumber + " from " + booking.Channel,
                    UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                    PersonId = person.PersonID,
                    TimeStamp = DateTime.Now,
                    AuditLogDetails = Common.Classes.Common.DetailedCompare<EF.Booking>(new EF.Booking(), booking)
                });
            }

            if (auditLogList.Any())
            {
                uow.Context.AuditLogs.AddRange(auditLogList);
                await uow.Context.SaveChangesAsync();
            }

            return result;
        }

        private void SyncLinkedUserMaster(EF.Person person, EF.Person oldPerson, AddPersonVM personVM)
        {
            var linkedUser = uow.GenericRepository<UserMaster>().Table
                .FirstOrDefault(x => x.PersonID == person.PersonID && x.IsEnable == true);

            if (linkedUser == null)
                return;

            bool emailChanged = !string.Equals(oldPerson.Email, person.Email, StringComparison.OrdinalIgnoreCase);
            bool nameChanged = oldPerson.FullName != person.FullName;
            bool phoneChanged = oldPerson.Phone != person.Phone;

            if (!emailChanged && !nameChanged && !phoneChanged)
                return;

            if (emailChanged)
            {
                bool emailTaken = uow.GenericRepository<UserMaster>().Table
                    .Any(x => x.IsEnable == true && x.IsStudent == true && x.Email == person.Email && x.ID != linkedUser.ID);
                if (emailTaken)
                    throw new Exception("Email already registered to another resident account.");

                linkedUser.Email = person.Email;
                linkedUser.Username = person.Email;
            }

            if (nameChanged)
                linkedUser.FullName = person.FullName;

            if (phoneChanged)
                linkedUser.Phone = person.Phone;

            linkedUser.UpdatedAt = personVM.UpdatedDate;
            linkedUser.UpdatedBy = personVM.UpdatedBy;

            uow.GenericRepository<UserMaster>().Update(linkedUser);
            uow.SaveChanges();
        }

        public EF.Person UpdatePerson(AddPersonVM personVM, HttpPostedFileBase file)
        {
            bool residentIdNumberExistForOtherPerson = uow.GenericRepository<EF.Person>().Table.Any(x => x.IsEnable == true && (x.ResidentID == personVM.ResidentID && personVM.ResidentID != null)
            && x.PersonID != personVM.PersonID && x.LocationId == personVM.LocationId);
            if (residentIdNumberExistForOtherPerson)
                throw new Exception("ResidentID Number already registered for another person.");


            //bool exist = uow.GenericRepository<EF.Person>().Table.Any(x => x.IsEnable == true && x.Email == personVM.Email && x.PersonID != personVM.PersonID && x.LocationId == personVM.LocationId);
            //if (exist)
            //    throw new Exception("Email already registered.");
            EF.Person oldperson = uow.GenericRepository<EF.Person>().GetByIdAsNoTracking(x => x.PersonID == personVM.PersonID);
            EF.Person person = GetPersonById(personVM.PersonID);
            if (person != null)
            {
                if (file != null)
                {
                    var result = Common.ImageUpload.SaveFile(file, "ProfilePhotos");
                    personVM.ImageUrl = "/Upload/Files/ProfilePhotos/" + result;
                    person.ImageUrl = personVM.ImageUrl; // Update ImageUrl only if a new file is provided
                }
                person.Title = personVM.Title;
                person.FullName = personVM.FullName;
                person.Email = personVM.Email;
                person.SecondaryEmail = personVM.SecondaryEmail;
                person.Phone = personVM.Phone;
                person.Gender = personVM.Gender;
                person.DOB = personVM.DOB;
                person.Nationality = personVM.Nationality;
                //person.Universiry = personVM.University;
                person.UniversityId = personVM.UniversityId;
                person.UpdatedDate = personVM.UpdatedDate;
                person.UpdatedBy = personVM.UpdatedBy;
                person.ResidentID = personVM.ResidentID;
                person.ReferralCode = personVM.ReferralCode;
                person.VehicleNumber = personVM.VehicleNumber;
                person.PassportNumber = personVM.PassportNumber;
                person.Religion = personVM.Religion;
                person.CampusEmail = personVM.CampusEmail;
                person.WhatsappNumber = personVM.ResidentWhatsappNumber;
                person.MercuryID = personVM.MercuryID;
                person.ProfileNotes = personVM.ProfileNotes;
                person.UniversityStudentID = personVM.UniversityStudentID;
                uow.GenericRepository<EF.Person>().Update(person);
                uow.SaveChanges();

                SyncLinkedUserMaster(person, oldperson, personVM);

                var emergencyContact = uow.GenericRepository<EmergencyContact>().Table.FirstOrDefault(ec => ec.PersonID == personVM.PersonID);

                if (!string.IsNullOrEmpty(personVM.GuardianFullName) && !string.IsNullOrEmpty(personVM.GuardianPhone))
                {
                    if (emergencyContact == null)
                    {
                        emergencyContact = new EmergencyContact
                        {
                            PersonID = personVM.PersonID,
                            FullName = personVM.GuardianFullName,
                            Phone = personVM.GuardianPhone,
                            Email = personVM.GuardianOtherEmail,
                            Relation = personVM.GuardianRelation,
                            PassportNumber = personVM.GuardianPassportNumber
                        };

                        uow.GenericRepository<EmergencyContact>().Insert(emergencyContact);
                    }
                    else
                    {
                        emergencyContact.FullName = personVM.GuardianFullName;
                        emergencyContact.Phone = personVM.GuardianPhone;
                        emergencyContact.Email = personVM.GuardianOtherEmail;
                        emergencyContact.Relation = personVM.GuardianRelation;
                        emergencyContact.PassportNumber = personVM.GuardianPassportNumber;

                        uow.GenericRepository<EmergencyContact>().Update(emergencyContact);
                    }

                    uow.SaveChanges();
                }

                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<EF.Person>(oldperson, person);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Update,
                        ActionId = (int)Enumeration.CorrespondenceAction.UpdatePerson,
                        PK = person.PersonID.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "Person",
                        Reference = person.Code.ToString(),
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        PersonId = person.PersonID,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }

                return person;
            }
            else
                throw new Exception("Person not found to update.");
        }

        public bool DeletePerson(int id)
        {
            EF.Person oldperson = uow.GenericRepository<EF.Person>().GetByIdAsNoTracking(x => x.PersonID == id);
            EF.Person person = GetPersonById(id);
            var bookingExist = uow.Context.Bookings.Where(x => x.PersonID == id && x.IsEnable == true).FirstOrDefault();
            if (bookingExist != null)
            {
                throw new Exception("Unable to delete profile. Booking is available; please delete booking first");
            }
            if (person != null)
            {
                person.IsEnable = false;

                uow.GenericRepository<EF.Person>().Update(person);
                uow.SaveChanges();

                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<EF.Person>(oldperson, person);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Delete,
                        ActionId = (int)Enumeration.CorrespondenceAction.DeletePerson,
                        PK = person.PersonID.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "Person",
                        Reference = person.Code.ToString(),
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        PersonId = person.PersonID,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }
                return true;
            }
            else
                throw new Exception("Person not found to delete.");
        }

        public List<AddPersonVM> UploadPersonByExcelFile(string filePath)
        {
            List<AddPersonVM> modelList = new List<AddPersonVM>();



            return modelList;
        }

        public List<PersonDocument> GetPersonDocuments()
        {
            return uow.GenericRepository<PersonDocument>().Table.ToList();
        }


        public string GetMaxPersonCode(int LocationId)
        {
            int code = 0;
            if (uow.GenericRepository<EF.Person>().Table.Where(x => x.Code != null && x.LocationId == LocationId).Count() != 0)
            {
                var nowithGRn = Convert.ToDecimal(uow.GenericRepository<EF.Person>().Table.Where(x => x.Code != null && x.LocationId == LocationId).AsEnumerable().Select(x => new { Number = Convert.ToDecimal(x.Code.Split('-').Last()) }).Max(x => x.Number)) + 1;
                code = (int)nowithGRn;
            }
            else

            {
                code = 1;
            }

            var data = uow.GenericRepository<EF.Location>().GetById(LocationId);
            var maxcode = code;
            string value = String.Format("{0:D4}", maxcode);
            var Code = "PER-" + data.Prefix + "-" + value;
            return Code;
        }

        // Generate next code using locking on the Person rows for a location
        private string GetNextPersonCodeWithLock(int locationId)
        {
            var data = uow.GenericRepository<EF.Location>().GetById(locationId);
            var prefix = data.Prefix;
            var maxVal = uow.Context.Database.SqlQuery<int>(
                "SELECT ISNULL(MAX(CAST(RIGHT(Code, 4) AS INT)), 0) FROM Person WITH (UPDLOCK, HOLDLOCK) WHERE LocationId = @p0",
                locationId).FirstOrDefault();
            var next = maxVal + 1;
            return "PER-" + prefix + "-" + next.ToString("D4");
        }

        private EF.University ResolveUniversity(Dictionary<string, EF.University> universityLookup, string universityName)
        {
            var normalizedUniversityName = NormalizeImportValue(universityName);
            return universityLookup.TryGetValue(normalizedUniversityName, out var university)
                ? university
                : null;
        }

        private EF.PriceConfig ResolvePriceConfig(Dictionary<string, EF.PriceConfig> priceConfigLookup, PMS.DTO.ViewModels.BookingViewModels.AddBookingVM booking)
        {
            var normalizedTerm = NormalizeImportValue(booking.TermName);
            var normalizedRoomType = NormalizeImportValue(booking.RoomTypeName);
            var comparableTerm = NormalizeComparableImportTerm(booking.TermName, booking.RoomTypeName);
            var importPrice = booking.ImportPrice ?? 0;

            if (priceConfigLookup.TryGetValue(BuildPriceConfigLookupKey(normalizedTerm, normalizedRoomType, importPrice), out var priceConfig))
                return priceConfig;

            return priceConfigLookup.TryGetValue(BuildPriceConfigLookupKey(comparableTerm, normalizedRoomType, importPrice), out priceConfig)
                ? priceConfig
                : null;
        }

        private int GetNextPersonSequenceWithLock(int locationId)
        {
            return uow.Context.Database.SqlQuery<int>(
                "SELECT ISNULL(MAX(CAST(RIGHT(Code, 4) AS INT)), 0) + 1 FROM Person WITH (UPDLOCK, HOLDLOCK) WHERE LocationId = @p0",
                locationId).FirstOrDefault();
        }

        private string FormatPersonCode(string prefix, int sequence)
        {
            return "PER-" + prefix + "-" + sequence.ToString("D4");
        }

        private bool MatchesImportPrice(decimal basePrice, decimal? cleaningCharge, decimal importPrice)
        {
            var totalWithCleaning = basePrice + (cleaningCharge ?? 0m);
            return basePrice == importPrice || totalWithCleaning == importPrice;
        }

        private Dictionary<string, EF.PriceConfig> BuildPriceConfigLookup(List<EF.PriceConfig> priceConfigs)
        {
            var lookup = new Dictionary<string, EF.PriceConfig>();

            foreach (var priceConfig in priceConfigs)
            {
                var roomType = NormalizeImportValue(priceConfig.RoomType?.RoomName);
                var termCandidates = new[]
                {
                    NormalizeImportValue(priceConfig.Term?.TermName),
                    NormalizeImportValue(priceConfig.Term?.TermDescription)
                }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

                var priceCandidates = new[]
                {
                    priceConfig.Price,
                    priceConfig.Price + (priceConfig.CleaningCharge ?? 0m)
                }
                .Distinct()
                .ToList();

                foreach (var term in termCandidates)
                {
                    foreach (var price in priceCandidates)
                    {
                        var key = BuildPriceConfigLookupKey(term, roomType, price);
                        if (!lookup.ContainsKey(key))
                            lookup.Add(key, priceConfig);
                    }
                }
            }

            return lookup;
        }

        private string NormalizeComparableImportTerm(string termName, string roomTypeName)
        {
            var normalizedTerm = NormalizeImportValue(termName);
            var normalizedRoomType = NormalizeImportValue(roomTypeName);

            if (string.IsNullOrWhiteSpace(normalizedTerm) || string.IsNullOrWhiteSpace(normalizedRoomType))
                return normalizedTerm;

            var suffix = " - " + normalizedRoomType;
            if (normalizedTerm.EndsWith(suffix))
                return normalizedTerm.Substring(0, normalizedTerm.Length - suffix.Length).Trim();

            return normalizedTerm;
        }

        private string BuildPriceConfigLookupKey(string normalizedTerm, string normalizedRoomType, decimal price)
        {
            return $"{normalizedTerm}|{normalizedRoomType}|{price:0.##}";
        }

        private string NormalizeImportValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
        }

        private string GetDefaultTitle(string gender)
        {
            if (string.IsNullOrWhiteSpace(gender))
                return string.Empty;

            return gender.Trim().Equals("Male", StringComparison.OrdinalIgnoreCase) ? "Mr." : "Ms.";
        }

        private void SetImportError(ImportPersonsWithBookingResultVM result, AddPersonVM personVM, string message)
        {
            personVM.ImportError = message;
            result.NotSavedPersons.Add(personVM);
            LogError(BuildImportErrorLogMessage(personVM, message));
        }

        private static string BuildImportErrorLogMessage(AddPersonVM personVM, string message)
        {
            var booking = personVM?.Booking;
            var bookingDetails = booking == null
                ? string.Empty
                : $", Term: {booking.TermName}, Room: {booking.RoomTypeName}, Price: {booking.ImportPrice}";

            return $"Person import failed: {message} Email: {personVM?.Email}, Name: {personVM?.FullName}, Location: {personVM?.LocationId}{bookingDetails}";
        }

        private void LogError(string message)
        {
            ErrorLogger.WriteToTestingLog("$ERROR:", message);
        }


        public bool GenrateUser(int Id)
        {
            var Person = uow.GenericRepository<EF.Person>().GetById(Id);
            if (Person == null)
            {
                throw new Exception("Person Does Not Exist!");
            }
            AddUserVM user = new AddUserVM();

            var role = uow.GenericRepository<EF.Role>().Table.Where(x => x.RoleName == "Student").FirstOrDefault();
            var createdBy = PMS.Common.Globals.User.Email;
            var Password = Membership.GeneratePassword(11, 2);
            var CreatedDate = DateTime.Now;

            if (role == null)
            {
                AddRoleVM roleVm = new AddRoleVM();
                roleVm.RoleName = "Student";
                roleVm.RoleDescription = "Student";
                roleVm.CreatedDate = CreatedDate;
                roleVm.CreatedBy = createdBy;
                role = userService.AddRole(roleVm);
            }


            user.CreatedBy = createdBy;
            user.CreatedDate = CreatedDate;
            user.FullName = Person.FullName;
            user.Email = Person.Email;
            user.Gender = Person.Gender;
            user.DOB = Person.DOB;
            user.IsStudent = true;
            user.IsActive = true;
            user.RoleId = role.RoleId;
            user.PersonID = Person.PersonID;
            user.Password = Password;
            user.Phone = Person.Phone;
            user.LocationId = Person.LocationId ?? 0;

            userService.AddUser(user);

            EmailMessagesListVM messagesByActionId = this.correspondenceService.GetEmailMessagesByActionId((int)Enumeration.CorrespondenceAction.CreateUserMaster, Person.LocationId ?? 0);
            if (messagesByActionId != null)
            {

                string body = messagesByActionId.EmailMessageBody.Replace("[[User_Email]]", user.Email);
                body = body.Replace("[[User_Password]]", user.Password);
                body = body.Replace("[[LoginLink]]", ConfigurationSettings.AppSettings.Get("BaseUrl"));
                try
                {
                    emailService.SendEmailAsync(Convert.ToString(messagesByActionId.EmailMessageSubject), body, true, user.Email, messagesByActionId.EmailMessageSenderID);
                }
                catch (Exception ex)
                {

                }

            }
            return true;
        }
        public AddUserVM CheckUserMaster(int id)
        {
            var user = uow.GenericRepository<UserMaster>().Table.Where(x => x.PersonID == id).Select(x => new AddUserVM
            {
                FullName = x.FullName,
                Email = x.Email,
                IsActive = x.IsActive,
                UserID = x.ID
            }).FirstOrDefault();
            return user;
        }
        public bool ResendEmail(int Userid)
        {
            var user = uow.GenericRepository<UserMaster>().GetById(Userid);
            if (user == null)
            {
                throw new Exception("User Not Found");
            }

            EmailMessagesListVM messagesByActionId = this.correspondenceService.GetEmailMessagesByActionId((int)Enumeration.CorrespondenceAction.CreateUserMaster, user.Person.LocationId ?? 0);
            var Password = Membership.GeneratePassword(11, 2);
            user.Password = PMS.Common.Security.StringCipher.Encrypt(Password);
            user.IsActive = true;
            if (messagesByActionId != null)
            {


                string body = messagesByActionId.EmailMessageBody.Replace("[[User_Email]]", user.Email);
                body = body.Replace("[[User_Password]]", Password);
                body = body.Replace("[[LoginLink]]", ConfigurationSettings.AppSettings["BaseUrl"]);
                try
                {
                    emailService.SendEmailAsync(Convert.ToString(messagesByActionId.EmailMessageSubject), body, true, user.Email, messagesByActionId.EmailMessageSenderID);
                }
                catch (Exception ex)
                {

                }

            }
            uow.SaveChanges();
            return true;
        }

        public InHousePortalCredentialsResultVM SendPortalCredentialsToInHouseResidents(int? locationId = null)
        {
            var result = new InHousePortalCredentialsResultVM
            {
                Details = new List<InHousePortalCredentialsDetailVM>()
            };

            var inHousePersons = GetInHouseResidents(locationId);
            result.TotalInHouseResidents = inHousePersons.Count;

            foreach (var person in inHousePersons)
            {
                var detail = new InHousePortalCredentialsDetailVM
                {
                    PersonID = person.PersonID,
                    FullName = person.FullName,
                    Email = person.Email,
                    Code = person.Code
                };

                try
                {
                    if (string.IsNullOrWhiteSpace(person.Email))
                    {
                        detail.Status = "Failed";
                        detail.Message = "Person email is missing.";
                        result.Failed++;
                        result.Details.Add(detail);
                        continue;
                    }

                    var existingUser = CheckUserMaster(person.PersonID);
                    if (existingUser == null)
                    {
                        //GenrateUser(person.PersonID);
                        detail.Status = "Created";
                        detail.Message = "Portal account created and credentials emailed.";
                        result.Created++;
                    }
                    else
                    {
                        //ResendEmail(existingUser.UserID);
                        detail.Status = "Resent";
                        detail.Message = "Portal credentials emailed.";
                        result.Resent++;
                    }
                }
                catch (Exception ex)
                {
                    detail.Status = "Failed";
                    detail.Message = ex.GetBaseException().Message;
                    result.Failed++;
                    LogError($"SendPortalCredentialsToInHouseResidents failed for PersonID {person.PersonID}: {detail.Message}");
                }

                result.Details.Add(detail);
            }

            return result;
        }

        private List<EF.Person> GetInHouseResidents(int? locationId)
        {
            var personIds = (from person in uow.GenericRepository<EF.Person>().Table
                             join booking in uow.GenericRepository<EF.Booking>().Table on person.PersonID equals booking.PersonID
                             join placement in uow.GenericRepository<EF.BedSpacePlacement>().Table on booking.BookingID equals placement.BookingID
                             where person.IsEnable == true
                                 && booking.IsEnable == true
                                 && placement.IsEnable == true
                                 && placement.CheckIn != null
                                 && placement.CheckOut == null
                                 && placement.GuestCount == null
                                 && (!locationId.HasValue || person.LocationId == locationId.Value)
                             select person.PersonID).Distinct().ToList();

            if (!personIds.Any())
                return new List<EF.Person>();

            return uow.GenericRepository<EF.Person>().Table
                .Where(p => personIds.Contains(p.PersonID))
                .ToList();
        }

        public List<AddPersonVM> GetReferrals(string referralcode)
        {
            //var location = uow.GenericRepository<EF.Booking>().Table.Where(x => x.HearFromCode == referralcode).Select(x=>x.LocationID).FirstOrDefault();
            //var referal = uow.GenericRepository<EF.LocationSetting>().Table.Where(x => x.LocationId==location).Select(x =>x.ReferralIsActive).FirstOrDefault();
            var db = uow.Context;
            var data = (from b in db.Bookings
                        join bsp in db.BedSpacePlacements
                        on b.BookingID equals bsp.BookingID
                        into bp
                        from BedSpacePlacements in bp.DefaultIfEmpty()
                        join cr in db.StudentCreditNotes
                        on b.BookingID equals cr.BookingId
                        into Cri
                        from StudentCreditNote in Cri.DefaultIfEmpty()
                        where (b.HearFromCode == referralcode)

                        select new
                        {
                            StudentCreditNote.BookingId,
                            StudentCreditNote.Type,
                            BedSpacePlacements.CheckIn,
                            b.Person.FullName,
                            b.Person.Email,
                            b.Person.Phone,
                            b.Person.Code,
                            b.Person.Title,
                            b.Person.Gender


                        }).Select(x => new AddPersonVM
                        {
                            FullName = x.FullName,
                            Email = x.Email,
                            Phone = x.Phone,
                            Code = x.Code,
                            Title = x.Title,
                            Gender = x.Gender,
                            Checkin = x.CheckIn,
                            BookingId = x.BookingId,
                            Type = x.Type



                        });
            var dat = data.ToList();
            return dat;
        }

        public IQueryable<EF.Person> GetPersonQueryable()
        {
            return uow.GenericRepository<EF.Person>().Table;
        }
    }
}
