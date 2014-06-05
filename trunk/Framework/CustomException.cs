using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework
{
  
    public class CustomException : ApplicationException
    {
        #region "Hard-Coded Error Messages"

        public static readonly string MESSAGE_GENERIC_ERROR =
            "Application has encountered an error, Please contact the Administrator";

        public static readonly string MESSAGE_UNKNOWN =
            "Application has encountered an error (message id: {0})";

        public static readonly string MESSAGE_SECURITY_ERROR =
            "You don't have permissions to view the page or " +
            "to perform the specific action.";
        public static readonly string MESSAGE_AUTHENTICATION = "Invalid email address and/or password.";

        public static readonly string MESSAGE_LOGIN_NEEDED =
            "You must log into the system to view the page or " +
            "to perform the specific action.";

        public static readonly string MESSAGE_DELETE_DATA_IN_USE =
            "Data cannot be deleted as it is used ";

        public static readonly string MESSAGE_DUPLICATE_DATA =
           "Duplicate data";

        #endregion
        public CustomException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public CustomException(string message)
            : base(message, null)
        {
        }

    }

    public class AuthenticationFailedException : CustomException
    {

        public AuthenticationFailedException()
            : base(MESSAGE_AUTHENTICATION, null)
        {
        }

        public AuthenticationFailedException(Exception innerException)
            : base(MESSAGE_AUTHENTICATION, innerException)
        {
        }
    }
    public class CommonDbException : CustomException
    {
        public CommonDbException(string message)
            : base(message)
        {
            Debug.Assert(message == MESSAGE_DELETE_DATA_IN_USE ||
                message == MESSAGE_DUPLICATE_DATA);
        }
    }
}
