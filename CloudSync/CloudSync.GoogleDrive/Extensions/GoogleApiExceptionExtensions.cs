using Google;

namespace CloudSync.GoogleDrive.Extensions;

public static class GoogleApiExceptionExtensions
{
    public static bool ContainsReason(this GoogleApiException ex, string reason)
    {
        return ex.Error.Errors.Any(e => e.Reason == reason);
    }
}