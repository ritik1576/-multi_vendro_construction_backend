using InframartAPI_New.DTOs.Vendor;

namespace InframartAPI_New.Services.Interfaces
{
    public interface IVendorService
    {
        string RegisterVendor(VendorRegisterDto dto);
        string UpdateVendor(VendorUpdateDto dto);
        string GetVendorById(int id);
    }
}