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

        public const string VarAccessMode = "accessMode";
        public const string VarAppType = "appType";
        public const string VarAttribute = "attribute";
        public const string VarAuthId = "authId";
        public const string VarCacheServer = "cacheServer";
        public const string VarCaseSensitive = "caseSensitive";
        public const string VarDataSrcId = "dataSrcId";
        public const string VarCompId = "compId";
        public const string VarCompVers = "compVers";
        public const string VarCompView = "compView";
        public const string VarContRep = "contRep";
        public const string VarContRepRef = "contRepRef";
        public const string VarDigest = "digest";
        public const string VarDocId = "docId";
        public const string VarDocIdRef = "docIdRef";
        public const string VarDocProt = "docProt";
        public const string VarDomain = "domain";
        public const string VarExpiration = "expiration";
        public const string VarFlags = "flags";
        public const string VarForceHeader = "forceHeader";
        public const string VarForceMimeType = "forceMimeType";
        public const string VarFromOffset = "fromOffset";
        public const string VarFromColumn = "fromColumn";
        public const string VarHashAlg = "hashalg";
        public const string VarIpAddr = "ipaddr";
        public const string VarIxAppl = "ixAppl";
        public const string VarIxCheckSum = "ixCheckSum";
        public const string VarIxUser = "ixUser";
        public const string VarOrigId = "origId";
        public const string VarLocker = "locker";
        public const string VarForce = "force";
        public const string VarName = "name";
        public const string VarNo206Response = "no206Response";
        public const string VarNumResults = "numResults";
        public const string VarPasswd = "passwd";
        public const string VarPattern = "pattern";
        public const string VarPermissions = "permissions";
        public const string VarPoolName = "poolName";
        public const string VarPrefVolume = "prefVolume";
        public const string VarStorageTier = "storageTier";
        public const string VarPVersion = "pVersion";
        public const string VarRelPath = "relPath";
        public const string VarReset = "reset";
        public const string VarResultAs = "resultAs";
        public const string VarRetention = "retention";
        public const string VarRmsPi = "rmspi";
        public const string VarRmsNode = "rmsnode";
        public const string VarSecKey = "secKey";
        public const string VarStoreParam = "storeParam";
        public const string VarTargetContRep = "targetContRep";
        public const string VarTenantId = "tenantId";
        public const string VarTimeout = "timeout";
        public const string VarToOffset = "toOffset";
        public const string VarToColumn = "toColumn";
        public const string VarToken = "token";
        public const string VarUser = "user";
        public const string VarUserSys = "userSys";
        public const string VarVolName = "volName";
        public const string VarWbDocId = "wbdocid";
        public const string VarScanned = "scanPerformed";
        public const string VarComponent = "component";
        public const string VarDocVersion = "docVersion";
        public const string VarRendition = "rendition";
        public const string VarAccessToken = "accessToken";
        public const string VarAddOnMode = "addonMode";
        public const string VarViewerMode = "viewerMode";
        public const string VarRecType = "recType";
        public const string VarRecInfo = "recInfo";
        public const string VarVolId = "volId";
        public const string VarFileName = "fileName";
        public const string VarContentLength = "contentLen";

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
