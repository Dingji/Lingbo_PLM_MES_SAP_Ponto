using MdmSyncService.Models;

namespace MdmSyncService.Services;

/// <summary>
/// Defines the contract for sending material master data to the MDM platform.
/// Implementations handle JSON serialization, HTTP transport, and response validation.
/// </summary>
public interface IMdmSyncService
{
    /// <summary>
    /// Sends a batch of material records to the MDM bulk-operation API.
    /// </summary>
    /// <param name="items">
    /// One or more material records to synchronize. Must not be null or empty.
    /// </param>
    /// <param name="cancellationToken">
    /// Token to observe for cancellation requests (e.g. service shutdown).
    /// </param>
    /// <returns>
    /// The parsed MDM response containing the status code and message.
    /// </returns>
    /// <exception cref="MdmSyncException">
    /// Thrown when the MDM platform returns a non-200 code, the response is malformed,
    /// or a transport-level error occurs that is not handled by the resilience pipeline.
    /// </exception>
    Task<MdmResponse> SendAsync(IReadOnlyList<MdmItem> items, CancellationToken cancellationToken = default);
}

/// <summary>
/// Exception thrown when the MDM synchronization operation fails at the business level
/// (i.e. the HTTP call succeeded but the MDM platform reported an error).
/// </summary>
public sealed class MdmSyncException : Exception
{
    /// <summary>
    /// The business status code returned by the MDM platform, if available.
    /// </summary>
    public int? MdmCode { get; }

    public MdmSyncException(string message, int? mdmCode = null, Exception? innerException = null)
        : base(message, innerException)
    {
        MdmCode = mdmCode;
    }
}
