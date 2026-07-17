using System;

namespace PMS.DTO.ViewModels.ContractViewModels
{
	public class ContractPlacementData
	{
		public int BookingId { get; set; }
		public int PersonId { get; set; }
		public int PlacementId { get; set; }
		public int? LocationId { get; set; }
		public int? PersonLocationId { get; set; }
		public string PersonFullName { get; set; }
		public string PersonCode { get; set; }
		public string PersonEmail { get; set; }
		public string PersonPhone { get; set; }
		public string PersonNationality { get; set; }
		public string UniversityName { get; set; }
		public string UniversityText { get; set; }
		public string RoomTypeName { get; set; }
		public string BuildingName { get; set; }
		public string FloorName { get; set; }
		public string RoomName { get; set; }
		public string BedName { get; set; }
		public DateTime CheckInDate { get; set; }
		public DateTime? CheckOutDate { get; set; }
		public string ECFullName { get; set; }
		public string ECRelation { get; set; }
		public string ECEmail { get; set; }
		public string ECPhone { get; set; }
	}
}


