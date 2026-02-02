namespace RecurringThings.Exceptions;

/// <summary>
/// Exception thrown when attempting to create an entry with a UID that already exists.
/// </summary>
public sealed class DuplicateUidException : Exception
{
    /// <summary>
    /// Gets the UID that caused the duplicate key violation.
    /// </summary>
    public string Uid { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateUidException"/> class.
    /// </summary>
    /// <param name="uid">The UID that already exists.</param>
    public DuplicateUidException(string uid)
        : base($"An entry with UID '{uid}' already exists.")
    {
        Uid = uid;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateUidException"/> class.
    /// </summary>
    /// <param name="uid">The UID that already exists.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public DuplicateUidException(string uid, Exception innerException)
        : base($"An entry with UID '{uid}' already exists.", innerException)
    {
        Uid = uid;
    }
}
