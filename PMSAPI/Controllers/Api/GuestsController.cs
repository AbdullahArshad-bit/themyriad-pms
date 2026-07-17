using PMS.DTO;
using PMS.DTO.ViewModels.PersonManageViewModels;
using PMS.Services.Services.Person;
using PMSAPI.Models;
using System;
using System.Linq;
using System.Web.Http;

namespace PMSAPI.Controllers.Api
{
    /// <summary>
    /// Guest / resident profile get and update. Reuses the existing PMS Person service.
    /// </summary>
    [RoutePrefix("integration/api/v1/properties")]
    public class GuestsController : IntegrationApiController
    {
        private readonly IPersonService personService;

        public GuestsController(IPersonService _personService)
        {
            personService = _personService;
        }

        [HttpGet]
        [Route("{propertyId:int}/guests/{guestId:int}")]
        public ApiResponse<GuestResponse> Get(int propertyId, int guestId)
        {
            try
            {
                var person = personService.GetPersonById(guestId);
                if (person == null)
                {
                    return NotFound<GuestResponse>("Guest not found.");
                }
                return Success(MapGuest(person));
            }
            catch (Exception ex)
            {
                return Fail<GuestResponse>(ex.GetBaseException().Message);
            }
        }

        [HttpPost]
        [Route("{propertyId:int}/guests")]
        public ApiResponse<GuestResponse> Create(int propertyId, CreateGuestRequest model)
        {
            try
            {
                if (model == null)
                {
                    return Fail<GuestResponse>("Request body is required.");
                }

                if (string.IsNullOrWhiteSpace(model.FullName))
                {
                    return Fail<GuestResponse>("FullName is required.");
                }

                if (string.IsNullOrWhiteSpace(model.Email))
                {
                    return Fail<GuestResponse>("Email is required.");
                }

                if (string.IsNullOrWhiteSpace(model.Phone))
                {
                    return Fail<GuestResponse>("Phone is required.");
                }

                var existing = personService.GetPersonQueryable()
                    .FirstOrDefault(p => p.LocationId == propertyId && p.Email == model.Email);

                if (existing != null)
                {
                    return Success(MapGuest(existing), "Guest already exists.");
                }

                var code = personService.GetMaxPersonCode(propertyId);
                var personVm = new AddPersonVM
                {
                    LocationId = propertyId,
                    Code = code,
                    Title = "Mr.",
                    FullName = model.FullName,
                    Email = model.Email,
                    Phone = model.Phone,
                    Gender = string.IsNullOrWhiteSpace(model.Gender) ? "Male" : model.Gender,
                    DOB = model.DateOfBirth ?? new DateTime(1990, 1, 1),
                    Nationality = string.IsNullOrWhiteSpace(model.Nationality) ? "Unknown" : model.Nationality,
                    PassportNumber = string.IsNullOrWhiteSpace(model.PassportNumber) ? "-" : model.PassportNumber,
                    ResidentWhatsappNumber = null,
                    UniversityId = 174,
                    CreatedDate = DateTime.Now,
                    CreatedBy = PMS.Common.Globals.User?.Email ?? "integration-api",
                    IsActive = true
                };

                var created = personService.AddPerson(personVm, null);
                if (created == null)
                {
                    return Fail<GuestResponse>("Failed to create guest profile.");
                }

                return Success(MapGuest(created), "Guest created.");
            }
            catch (Exception ex)
            {
                return Fail<GuestResponse>(ex.GetBaseException().Message);
            }
        }

        [HttpPut]
        [Route("{propertyId:int}/guests/{guestId:int}")]
        public ApiResponse<GuestResponse> Update(int propertyId, int guestId, UpdateGuestRequest model)
        {
            try
            {
                if (model == null)
                {
                    return Fail<GuestResponse>("Request body is required.");
                }

                var person = personService.GetPersonById(guestId);
                if (person == null)
                {
                    return NotFound<GuestResponse>("Guest not found.");
                }

                // Build the update VM from the current record, then apply only supplied changes
                // so unrelated fields are preserved.
                var vm = new AddPersonVM
                {
                    PersonID = person.PersonID,
                    LocationId = person.LocationId ?? propertyId,
                    Code = person.Code,
                    ReferralCode = person.ReferralCode,
                    Title = person.Title,
                    FullName = model.FullName ?? person.FullName,
                    Email = model.Email ?? person.Email,
                    SecondaryEmail = model.SecondaryEmail ?? person.SecondaryEmail,
                    CampusEmail = person.CampusEmail,
                    Religion = person.Religion,
                    PassportNumber = model.PassportNumber,
                    Phone = model.Phone ?? person.Phone,
                    Gender = person.Gender,
                    DOB = person.DOB,
                    Nationality = model.Nationality ?? person.Nationality,
                    UniversityId = person.UniversityId ?? 0,
                    ImageUrl = person.ImageUrl,
                    ResidentWhatsappNumber = model.WhatsappNumber ?? person.WhatsappNumber,
                    UpdatedDate = DateTime.Now,
                    UpdatedBy = PMS.Common.Globals.User.Email
                };

                var updated = personService.UpdatePerson(vm, null);
                if (updated == null)
                {
                    return Fail<GuestResponse>("Guest update failed.");
                }

                return Success(MapGuest(updated), "Guest updated.");
            }
            catch (Exception ex)
            {
                return Fail<GuestResponse>(ex.GetBaseException().Message);
            }
        }

        private static GuestResponse MapGuest(PMS.EF.Person person)
        {
            return new GuestResponse
            {
                Id = person.PersonID,
                FullName = person.FullName,
                Email = person.Email,
                SecondaryEmail = person.SecondaryEmail,
                Phone = person.Phone,
                WhatsappNumber = person.WhatsappNumber,
                Gender = person.Gender,
                Nationality = person.Nationality,
                PassportNumber = person.PassportNumber,
                ResidentId = person.ResidentID,
                Code = person.Code,
                PropertyId = person.LocationId
            };
        }
    }
}
