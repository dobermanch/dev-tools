namespace Dev.Tools.DataFormats;

/// <summary>
/// The input is not valid in the format it was said to be in. The message is for whoever debugs a
/// reader; a tool reports the failure as an error code and never shows it.
/// </summary>
internal sealed class UnreadableDataException(string message, Exception? innerException = null)
    : Exception(message, innerException);

/// <summary>
/// The data is valid, but the target format has no way to write it - a null in TOML, CSV from anything
/// but an array of objects. A writer reports it rather than changing the data to fit.
/// </summary>
internal sealed class UnwritableDataException(string message, Exception? innerException = null)
    : Exception(message, innerException);
