using System.Globalization;

namespace SAPArchiveLink
{
    public interface IAccessIdentifier
    {
        /// <summary>
        /// Indicates if request was received over SSL.
        /// </summary>
        /// <returns>True if SSL was used, false otherwise.</returns>
        bool GetSSLMode();

        /// <summary>
        /// Sets the user name for accounting purposes.
        /// </summary>
        void SetUserName(string userName);

        /// <summary>
        /// Returns the user name.
        /// </summary>
        string GetUserName();

        /// <summary>
        /// Sets the application name for accounting purposes.
        /// </summary>
        void SetApplicationName(string applicationName);

        /// <summary>
        /// Returns the application name.
        /// </summary>
        string GetApplicationName();

        /// <summary>
        /// Sets the locale to localize messages.
        /// </summary>
        void SetLocale(CultureInfo locale);

        /// <summary>
        /// Returns the locale or generates one from "Accept-Language" header if not set.
        /// </summary>
        CultureInfo GetLocale();

        /// <summary>
        /// Returns the evaluated language as string.
        /// </summary>
        string GetLanguage();

        /// <summary>
        /// Returns object holding request-specific data.
        /// </summary>
        /// <param name="createIfNotExist">If true, create if not exists.</param>
        /// <returns>Request-specific data or null.</returns>
       // ICSRequestSpecificData GetRequestSpecificData(bool createIfNotExist);

        /// <summary>
        /// Returns the IP address of the original client.
        /// </summary>
        string GetClientIP();

        /// <summary>
        /// Sets the client IP to the local host address.
        /// </summary>
        void SetClientIP();

        /// <summary>
        /// Sets the client IP to the specified address.
        /// </summary>
        void SetClientIP(string address);

        /// <summary>
        /// Returns the remote address (set by AL).
        /// </summary>
        string GetRemoteAddress();

        /// <summary>
        /// Gets the access token.
        /// </summary>
        string GetAccessToken();

        /// <summary>
        /// Sets the access token (used in signed URLs).
        /// </summary>
        void SetAccessToken(string accessToken);

        /// <summary>
        /// Forces the request to be signed regardless of archive settings.
        /// </summary>
        void ForceSignature(bool enforced);

        /// <summary>
        /// Sets the document protection flag (subset of "crud").
        /// </summary>
        void SetDocumentProtection(string protection);

        /// <summary>
        /// Gets the document protection.
        /// </summary>
        string GetDocumentProtection();

        /// <summary>
        /// Sets the URL path extension (e.g. jsessionid).
        /// </summary>
        void SetUrlPathExtension(string urlPathExtension);

        /// <summary>
        /// Gets the URL path extension.
        /// </summary>
        string GetUrlPathExtension();

        /// <summary>
        /// Sets the originator ID (for quota/accounting).
        /// </summary>
        void SetOrigId(string origId);

        /// <summary>
        /// Gets the originator ID.
        /// </summary>
        string GetOrigId();

        /// <summary>
        /// Checks if request is from a privileged user.
        /// </summary>
        bool IsPrivilegedUser();
    }
}
