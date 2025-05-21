using System.Text;
using System.Web;

namespace SAPArchiveLink
{
    public class ALParameter : ICommandParameter
    {
        private readonly ALParameterDefinition _definition;
        private readonly List<string> _list;
        private string _value;
        private string _encodedValue;
        private bool _isToSign;
        private List<string> _encodedList;

        public ALParameter(ALParameterDefinition definition, bool toSign = false)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _list = _definition.IsMultiVal ? new List<string>() : null;
            _isToSign = toSign;
        }

        public static readonly string VarAccessMode = "accessMode";
        public static readonly string VarAppType = "appType";
        public static readonly string VarAttribute = "attribute";
        public static readonly string VarAuthId = "authId";
        public static readonly string VarCacheServer = "cacheServer";
        public static readonly string VarCaseSensitive = "caseSensitive";
        public static readonly string VarDataSrcId = "dataSrcId";
        public static readonly string VarCompId = "compId";
        public static readonly string VarCompVers = "compVers";
        public static readonly string VarCompView = "compView";
        public static readonly string VarContRep = "contRep";
        public static readonly string VarContRepRef = "contRepRef";
        public static readonly string VarDigest = "digest";
        public static readonly string VarDocId = "docId";
        public static readonly string VarDocIdRef = "docIdRef";
        public static readonly string VarDocProt = "docProt";
        public static readonly string VarDomain = "domain";
        public static readonly string VarExpiration = "expiration";
        public static readonly string VarFlags = "flags";
        public static readonly string VarForceHeader = "forceHeader";
        public static readonly string VarForceMimeType = "forceMimeType";
        public static readonly string VarFromOffset = "fromOffset";
        public static readonly string VarFromColumn = "fromColumn";
        public static readonly string VarHashAlg = "hashalg";
        public static readonly string VarIpAddr = "ipaddr";
        public static readonly string VarIxAppl = "ixAppl";
        public static readonly string VarIxCheckSum = "ixCheckSum";
        public static readonly string VarIxUser = "ixUser";
        public static readonly string VarOrigId = "origId";
        public static readonly string VarLocker = "locker";
        public static readonly string VarForce = "force";
        public static readonly string VarName = "name";
        public static readonly string VarNo206Response = "no206Response";
        public static readonly string VarNumResults = "numResults";
        public static readonly string VarPasswd = "passwd";
        public static readonly string VarPattern = "pattern";
        public static readonly string VarPermissions = "permissions";
        public static readonly string VarPoolName = "poolName";
        public static readonly string VarPrefVolume = "prefVolume";
        public static readonly string VarStorageTier = "storageTier";
        public static readonly string VarPVersion = "pVersion";
        public static readonly string VarRelPath = "relPath";
        public static readonly string VarReset = "reset";
        public static readonly string VarResultAs = "resultAs";
        public static readonly string VarRetention = "retention";
        public static readonly string VarRmsPi = "rmspi";
        public static readonly string VarRmsNode = "rmsnode";
        public static readonly string VarSecKey = "secKey";
        public static readonly string VarStoreParam = "storeParam";
        public static readonly string VarTargetContRep = "targetContRep";
        public static readonly string VarTenantId = "tenantId";
        public static readonly string VarTimeout = "timeout";
        public static readonly string VarToOffset = "toOffset";
        public static readonly string VarToColumn = "toColumn";
        public static readonly string VarToken = "token";
        public static readonly string VarUser = "user";
        public static readonly string VarUserSys = "userSys";
        public static readonly string VarVolName = "volName";
        public static readonly string VarWbDocId = "wbdocid";
        public static readonly string VarScanned = "scanPerformed";
        public static readonly string VarComponent = "component";
        public static readonly string VarDocVersion = "docVersion";
        public static readonly string VarRendition = "rendition";
        public static readonly string VarAccessToken = "accessToken";
        public static readonly string VarAddOnMode = "addonMode";
        public static readonly string VarViewerMode = "viewerMode";
        public static readonly string VarRecType = "recType";
        public static readonly string VarRecInfo = "recInfo";
        public static readonly string VarVolId = "volId";
        public static readonly string VarFileName = "fileName";
        public static readonly string VarContentLength = "contentLen";

        private const string PatternCodingMarker = "\u20AC1_"; // Euro-sign

        public string EncodeSearchPattern(string urlEncodedPattern)
        {
            const string MN = nameof(EncodeSearchPattern);
            string decoded = HttpUtility.UrlDecode(urlEncodedPattern);
            string base64Encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(decoded));
            string value = PatternCodingMarker + base64Encoded;
            return value;
        }

        public string DecodeSearchPattern(string encodedPattern, string charset)
        {
            const string MN = nameof(DecodeSearchPattern);
            if (!encodedPattern.StartsWith(PatternCodingMarker))
            {
                return encodedPattern;
            }

            string base64Value = encodedPattern.Substring(PatternCodingMarker.Length);
            try
            {
                byte[] decodedBytes = Convert.FromBase64String(base64Value);
                string value = string.IsNullOrEmpty(charset) ? Encoding.UTF8.GetString(decodedBytes) : Encoding.GetEncoding(charset).GetString(decodedBytes);
                return value;
            }
            catch (FormatException)
            {
                return encodedPattern;
            }
            catch (Exception ex) when (ex is ArgumentException || ex is DecoderFallbackException)
            {
                return encodedPattern;
            }
        }

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
                    _encodedValue = _value;
                }
            }
            return _encodedValue;
        }

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

        public bool IsMultiValue()
        {
            return _list != null;
        }

        public bool IsSameParam(ALParameter other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other == null) return false;
            if (_isToSign != other._isToSign) return false;
            if (_definition.Name != other._definition.Name) return false;
            if (_list != other._list && (_list == null || other._list == null)) return false;
            return true;
        }

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

        public List<string> GetValues()
        {
            return _list;
        }

        public string GetValue()
        {
            return _value;
        }

        public bool IsSet()
        {
            return _list == null ? _value != null : _list.Count > 0;
        }

        public string GetName()
        {
            return _definition.Name;
        }

        public bool IsMandatory()
        {
            return _definition.IsMandatory;
        }

        public void SetToSign(bool toSign)
        {
            _isToSign = toSign;
        }

        public bool IsToSign()
        {
            return _isToSign;
        }

        public object Clone()
        {
            var parameter = new ALParameter(_definition, _isToSign);
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

        public bool CheckValue()
        {
            bool isOkay = true;
            // Check routine logic can be added here if needed
            return isOkay;
        }

        public override string ToString()
        {
            return $"ALParameter [_name={_definition.Name}, _list={(_list != null ? string.Join(",", _list) : "null")}, _value={_value}, _isToSign={_isToSign}]";
        }
    }
}
