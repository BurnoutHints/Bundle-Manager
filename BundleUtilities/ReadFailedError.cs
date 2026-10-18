using System;

namespace BundleUtilities;

public class ReadFailedError : Exception
{
    public ReadFailedError(string msg) : base(msg)
    {

    }

    public ReadFailedError(string msg, Exception innerException) : base(msg, innerException)
    {

    }
}
