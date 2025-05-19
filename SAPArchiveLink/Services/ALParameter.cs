using System.Text;
using System.Web;

namespace SAPArchiveLink
{
    public class ALParameter : ICommandParameter
    {
        //private readonly ALParameterEnum _template;
        private readonly List<string> _list;
        private string _value;
        private string _encodedValue;
        private bool _isToSign;
        private List<string> _encodedList;

        // Logging context (replacing Java's Log.Context)
        public ALParameter(/*ALParameterEnum template*/ bool toSign = false)
        {
            //_template = template;
            _list = /*_template.IsMultiVal ?*/ new List<string>() /*: null*/;
            _isToSign = toSign;
        }

        public static readonly string VarAccessMode = "accessMode"; // Access mode -> secKey
        public static readonly string VarAppType = "appType"; // Application type -> used in write request for pool selection
        public static readonly string VarAttribute = "attribute"; // User attributes to document/component
        public static readonly string VarAuthId = "authId"; // Signer name -> secKey + id for certificate
        public static readonly string VarCacheServer = "cacheServer"; // Indicates cache server
        public static readonly string VarCaseSensitive = "caseSensitive"; // Case sensitive -> search requests
        public static readonly string VarDataSrcId = "dataSrcId"; // CMIS data source id used for billing purpose on AS/AC
        public static readonly string VarCompId = "compId"; // Component identifier or component name
        public static readonly string VarCompVers = "compVers"; // Component version (no longer supported)
        public static readonly string VarCompView = "compView"; // TODO: ??
        public static readonly string VarContRep = "contRep"; // Logical archive
        public static readonly string VarContRepRef = "contRepRef"; // TODO: ??
        public static readonly string VarDigest = "digest"; // Digest to hand from client to server, see verifyATS, verifySig
        public static readonly string VarDocId = "docId"; // Document identifier
        public static readonly string VarDocIdRef = "docIdRef"; // TODO: ??
        public static readonly string VarDocProt = "docProt"; // Document protection -> set by SAP (create requests)
        public static readonly string VarDomain = "domain"; // TODO: ??
        public static readonly string VarExpiration = "expiration"; // Expiration of signature -> secKey
        public static readonly string VarFlags = "flags"; // Multiple purpose setting
        public static readonly string VarForceHeader = "forceHeader"; // TODO: ??
        public static readonly string VarForceMimeType = "forceMimeType"; // Return component as contentType
        public static readonly string VarFromOffset = "fromOffset"; // From offset for "range" requests
        public static readonly string VarFromColumn = "fromColumn"; // From start column for search request
        public static readonly string VarHashAlg = "hashalg"; // Hash algorithm for getATS, verifyATS
        public static readonly string VarIpAddr = "ipaddr"; // IP address of client for csrvInfo
        public static readonly string VarIxAppl = "ixAppl"; // Application for book keeping purpose
        public static readonly string VarIxCheckSum = "ixCheckSum"; // Check sum from client
        public static readonly string VarIxUser = "ixUser"; // User name for book keeping purpose
        public static readonly string VarOrigId = "origId"; // Originator of request (quota will be checked)
        public static readonly string VarLocker = "locker"; // Unique string for lock/unlock operations
        public static readonly string VarForce = "force"; // Allows to unlock any lock previously locked by lock command
        public static readonly string VarName = "name"; // TODO: ??
        public static readonly string VarNo206Response = "no206Response"; // Indicate that instead of HTTP code 206, return 200
        public static readonly string VarNumResults = "numResults"; // TODO: ??
        public static readonly string VarPasswd = "passwd"; // Password for validUser request
        public static readonly string VarPattern = "pattern"; // Search pattern
        public static readonly string VarPermissions = "permissions"; // URL param defines default permissions for certificate uploaded via putCert
        public static readonly string VarPoolName = "poolName"; // Name of pool for create requests
        public static readonly string VarPrefVolume = "prefVolume"; // Names of preferred volumes for distribute content requests
        public static readonly string VarStorageTier = "storageTier"; // URL parameter storage tier for choosing pool
        public static readonly string VarPVersion = "pVersion"; // AL version of request
        public static readonly string VarRelPath = "relPath"; // Relative path for cluster distribution calls
        public static readonly string VarReset = "reset"; // Reset interruptible request, e.g., verifyATS
        public static readonly string VarResultAs = "resultAs"; // Preference for response format
        public static readonly string VarRetention = "retention"; // Retention parameter
        public static readonly string VarRmsPi = "rmspi"; // Contains information needed for RMS protection on download
        public static readonly string VarRmsNode = "rmsnode"; // Contains information needed for RMS protection on upload
        public static readonly string VarSecKey = "secKey"; // Signature of request
        public static readonly string VarStoreParam = "storeParam"; // For delayed writing from disk pool
        public static readonly string VarTargetContRep = "targetContRep"; // Target archive id for migrate call
        public static readonly string VarTenantId = "tenantId"; // Tenant id usable in all requests
        public static readonly string VarTimeout = "timeout"; // Timeout for interruptible request, e.g., verifyATS
        public static readonly string VarToOffset = "toOffset"; // To offset for "range" requests
        public static readonly string VarToColumn = "toColumn"; // To end column for search request
        public static readonly string VarToken = "token"; // Token for continuing interruptible request or deleting documents
        public static readonly string VarUser = "user"; // User name for validUser request
        public static readonly string VarUserSys = "userSys"; // System used to validate for validUser request
        public static readonly string VarVolName = "volName"; // Name of volume for create requests
        public static readonly string VarWbDocId = "wbdocid"; // DS uses this to indicate successful retrieval from cache server
        public static readonly string VarScanned = "scanPerformed"; // Sent from new SAP clients speaking 0047 protocol version

        // Additional context server parameters
        public static readonly string VarComponent = "component"; // Component
        public static readonly string VarDocVersion = "docVersion"; // Document version
        public static readonly string VarRendition = "rendition"; // Rendition
        public static readonly string VarAccessToken = "accessToken"; // Access token
        public static readonly string VarAddOnMode = "addonMode"; // Add-on mode
        public static readonly string VarViewerMode = "viewerMode"; // Viewer mode
        public static readonly string VarRecType = "recType"; // Record type
        public static readonly string VarRecInfo = "recInfo"; // Record info

        // Parameters used to get raw content from a cluster node
        public static readonly string VarVolId = "volId"; // ID of volume
        public static readonly string VarFileName = "fileName"; // Base name of the file
        public static readonly string VarContentLength = "contentLen"; // Content length

        // Pattern coding marker for search patterns
        private const string PatternCodingMarker = "\u20AC1_"; // Euro-sign

        // Encode a search pattern (URL decode -> Base64 encode)
        public string EncodeSearchPattern(string urlEncodedPattern)
        {
            const string MN = nameof(EncodeSearchPattern);

            // URL decode the pattern and Base64 encode it
            string decoded = HttpUtility.UrlDecode(urlEncodedPattern);
            string base64Encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(decoded));
            string value = PatternCodingMarker + base64Encoded;
            return value;
        }

        // Decode a search pattern (Base64 decode -> convert to string with specified charset)
        public string DecodeSearchPattern(string encodedPattern, string charset)
        {
            const string MN = nameof(DecodeSearchPattern);

            // Check if the pattern starts with the coding marker
            if (!encodedPattern.StartsWith(PatternCodingMarker))
            {
                return encodedPattern;
            }

            // Remove the marker and decode
            string base64Value = encodedPattern.Substring(PatternCodingMarker.Length);
            try
            {
                byte[] decodedBytes = Convert.FromBase64String(base64Value);
                string value = string.IsNullOrEmpty(charset) ? Encoding.UTF8.GetString(decodedBytes) : Encoding.GetEncoding(charset).GetString(decodedBytes);
                return value;
            }
            catch (FormatException ex)
            {
                return encodedPattern;
            }
            catch (Exception ex) when (ex is ArgumentException || ex is DecoderFallbackException)
            {
                return encodedPattern;
            }
        }

        // Get the encoded value for a single-value parameter
        public string GetEncodedValue(string charset)
        {
            const string MN = nameof(GetEncodedValue);
            if (_encodedValue == null && _value != null)
            {
                try
                {
                    _encodedValue = HttpUtility.UrlEncode(_value, string.IsNullOrEmpty(charset) ? Encoding.UTF8 : Encoding.GetEncoding(charset));
                }
                catch (Exception ex) when (ex is ArgumentException || ex is EncoderFallbackException)
                {
                    _encodedValue = _value; // Fallback to raw value
                }
            }
            return _encodedValue;
        }

        // Get the list of encoded values for a multi-value parameter
        public List<string> GetEncodedValues(string charset)
        {
            const string MN = nameof(GetEncodedValues);
            if (_encodedList == null && _list != null)
            {
                _encodedList = new List<string>();
                foreach (string value in _list)
                {
                    string encodedValue = value;
                    try
                    {
                        encodedValue = HttpUtility.UrlEncode(value, string.IsNullOrEmpty(charset) ? Encoding.UTF8 : Encoding.GetEncoding(charset));
                    }
                    catch (Exception ex) when (ex is ArgumentException || ex is EncoderFallbackException)
                    {
                    }
                    _encodedList.Add(encodedValue);
                }
            }
            return _encodedList;
        }

        // Check if the parameter is multi-value
        public bool IsMultiValue()
        {
            return _list != null;
        }

        // Check if two parameters are the same (based on name, toSign, and multi-value status)
        public bool IsSameParam(ALParameter other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other == null) return false;
            if (_isToSign != other._isToSign) return false;
            //if (_template.Name != other._template.Name) return false;
            if (_list != other._list && (_list == null || other._list == null)) return false;
            return true;
        }

        // Set the value for a single-value or multi-value parameter
        internal void SetValue(string value)
        {
            if (_list == null)
            {
                _value = value;
                _encodedValue = null;
            }
            else
            {
                _list.Add(value);
                _encodedList = null;
            }
        }

        // Get the list of values (or null for a single-value parameter)
        public List<string> GetValues()
        {
            return _list;
        }

        // Get the value of the parameter (implements IALParameter)
        public string GetValue()
        {
            return _value;
        }

        // Check if the parameter is set
        public bool IsSet()
        {
            return _list == null ? _value != null : _list.Count > 0;
        }

        // Get the name of the parameter
        //public string GetName()
        //{
        //    return _template.Name;
        //}

        //// Check if the parameter is mandatory
        //public bool IsMandatory()
        //{
        //    return _template.IsMandatory;
        //}

        // Set whether the parameter needs to be signed
        public void SetToSign(bool toSign)
        {
            _isToSign = toSign;
        }

        // Check if the parameter needs to be signed
        public bool IsToSign()
        {
            return _isToSign;
        }

        // Clone the parameter
        public object Clone()
        {
            var parameter = new ALParameter(/*_template,*/ _isToSign);
            if (_list == null)
            {
                parameter.SetValue(_value);
            }
            else
            {
                foreach (string value in _list)
                {
                    parameter.SetValue(value);
                }
            }
            return parameter;
        }

        // Validate the parameter's value(s) using the template's check routine
        public bool CheckValue()
        {
            bool isOkay = true;
            //if (_template.CheckRoutine != null)
            //{
            //    if (_list == null)
            //    {
            //        isOkay = _template.VerifyValue(_value);
            //    }
            //    else
            //    {
            //        foreach (string entry in _list)
            //        {
            //            isOkay = _template.VerifyValue(entry);
            //            if (!isOkay) break;
            //        }
            //    }
            //}
            return isOkay;
        }

        // Override ToString for debugging
        public override string ToString()
        {
            return $"ALParameter [_template=_template.Name, _list={(_list != null ? string.Join(",", _list) : "null")}, _value={_value}, _isToSign={_isToSign}]";
        }
    }
}
