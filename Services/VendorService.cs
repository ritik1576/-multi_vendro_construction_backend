using InframartAPI_New.DTOs.Vendor;
using InframartAPI_New.Services.Interfaces;

namespace InframartAPI_New.Services
{
    public class VendorService : IVendorService
    {
        public string RegisterVendor(VendorRegisterDto dto)
        {
            if (dto == null)
                return "Invalid request";

            if (string.IsNullOrEmpty(dto.BusinessName))
                return "Business Name required";

            // TODO: Save to DB later

            return $"Vendor Registered Successfully: {dto.BusinessName}";
        }

        public string UpdateVendor(VendorUpdateDto dto)
        {
            if (dto == null)
                return "Invalid request";

            // TODO: Update DB later

            return $"Vendor Updated Successfully: {dto.Id}";
        }

        public string GetVendorById(int id)
        {
            if (id <= 0)
                return "Invalid Vendor Id";

            // TODO: Fetch from DB later

            return $"Vendor Details for Id: {id}";
        }
    }
}