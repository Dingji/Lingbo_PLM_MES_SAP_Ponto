namespace FileSyncService;

public static class AppConstants
{
    public const int MaxStoreRecords = 500;
    public const int DefaultTimeoutSeconds = 120;
    public const int DefaultDownloadTimeoutSeconds = 120;
    public const int MaxConcurrentDownloads = 10;
    public const long MaxFileSizeBytes = 536870912;
    public const string DefaultRootDirectory = "D:\\FileSyncRoot";
    public const string DefaultMesEndpoint = "http://10.170.9.17:8888/ims-integrate/api/updateImsData";
    public const string OrgCode = "3701";
    public const string DocTypeFile = "CUST_FILE";
    public const string UpdateType = "UPDATE";
    public const string DefaultRootDir = "D:\\FileSyncRoot";
}
