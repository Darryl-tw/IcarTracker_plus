namespace TrackerPlus.Core.Interfaces.Repositories;

public interface ISubAdminRepository
{
    /// <summary>驗證子帳號可否執行裝置搬移（對齊舊 CheckSubadmin + MoveDevice 權限）。</summary>
    Task<int?> ValidateMoveDeviceAsync(string subAdminId, string subAdminPassword, bool defaultPay, int saleModel);

    /// <summary>驗證子帳號可否刪除裝置（DelDevice=1）。</summary>
    Task<int?> ValidateDeleteDeviceAsync(string subAdminId, string subAdminPassword);

    /// <summary>驗證子帳號可否新增裝置（InsertDevice=1）。</summary>
    Task<int?> ValidateInsertDeviceAsync(string subAdminId, string subAdminPassword);
}
