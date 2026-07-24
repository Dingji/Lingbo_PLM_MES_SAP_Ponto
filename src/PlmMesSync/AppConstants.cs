namespace PlmMesSync;

public static class AppConstants
{
    public const string OrgCode = "3701";
    public const string DocTypeBom = "BS_BOM";
    public const string DocTypeFile = "CUST_FILE";
    public const string UpdateType = "UPDATE";
    public const string IsMainYes = "y";
    public const string IsMainNo = "n";
    public const int DefaultDelaySeconds = 15;
    public const int DefaultFileSyncDelaySeconds = 15;
    public const int DefaultRetryCount = 3;
    public const int MaxStoreRecords = 2000;
    public const int DefaultHttpTimeoutSeconds = 60;
    public const int DefaultMesTimeoutSeconds = 120;
    public const string DefaultMesEndpoint = "http://10.170.9.17:8888/ims-integrate/api/updateImsData";
    public const string DefaultFileSyncServiceUrl = "http://10.170.9.4:31457/fileSync";
    public const string DefaultFileSyncDashboardUrl = "http://10.170.9.4:31457";
    public const string TableBom = "BOM";
    public const string TableFiles = "FILES";
    public const string SubstitutePriorityPrimary = "1";
}
