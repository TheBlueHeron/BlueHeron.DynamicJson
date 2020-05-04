Imports System.Security.Cryptography
Imports System.Text

''' <summary>
''' 
''' </summary>
Public Class OAuth_1

#Region " Objects and variables "

	Public Const Twitter_RequestTokenUrl As String = "https://api.twitter.com/oauth/request_token"
	Public Const Twitter_AuthorizeUrl As String = "https://api.twitter.com/oauth/authorize"
	Public Const Twitter_AccessTokenUrl = "https://api.twitter.com/oauth/access_token"

	Private m_CK As String
	Private m_CS As String
	Private m_ScreenName As String
	Private m_Secret As String
	Private m_Token As String
	Private m_UserID As Long?

#End Region

#Region " Properties "

	Public Overridable ReadOnly Property ConsumerKey As String
		Get
			Return m_CK
		End Get
	End Property

	Public Overridable ReadOnly Property ConsumerSecret As String
		Get
			Return m_CS
		End Get
	End Property

	Public ReadOnly Property ScreenName As String
		Get
			Return m_ScreenName
		End Get
	End Property

	Public Property Secret As String
		Get
			Return m_Secret
		End Get
		Set(value As String)
			m_Secret = value
		End Set
	End Property

	Public Property Token As String
		Get
			Return m_Token
		End Get
		Set(value As String)
			m_Token = value
		End Set
	End Property

	Public ReadOnly Property UserID As Long?
		Get
			Return m_UserID
		End Get
	End Property

#End Region

#Region " Public methods and functions "

	Public Function [Get](url As String, parameters As IEnumerable(Of KeyValuePair(Of String, String))) As DynamicResult
		Dim params As SortedDictionary(Of String, String) = GenerateOAuthParameters(m_Token)

		For Each p As KeyValuePair(Of String, String) In parameters
			params.Add(p.Key, p.Value)
		Next

		Dim signature As String = GenerateSignature(m_Secret, "GET", url, params)

		params.Add("oauth_signature", Http.UrlEncode(signature))

		Return Http.GetJson(url, params)

	End Function

	Public Function GetAccessToken(token As String, pin As String) As Boolean

		Try
			Dim uId As Long = 0
			Dim parameters As SortedDictionary(Of String, String) = GenerateOAuthParameters(token)

			parameters.Add("oauth_verifier", pin)

			Dim signature As String = GenerateSignature(m_Secret, "GET", Twitter_AccessTokenUrl, parameters)

			parameters.Add("oauth_signature", Http.UrlEncode(signature))

			Dim response As String = Http.Get(Twitter_AccessTokenUrl, parameters)
			Dim dic As Dictionary(Of String, String) = ParseResponse(response)

			If dic.ContainsKey("oauth_token") AndAlso dic.ContainsKey("oauth_token_secret") AndAlso Int64.TryParse(dic("user_id"), uId) Then
				m_Token = dic("oauth_token")
				m_Secret = dic("oauth_token_secret")
				m_ScreenName = dic("screen_name")
				m_UserID = uId

				Return True
			End If
		Catch ex As Exception
		End Try

		m_UserID = 0
		m_ScreenName = String.Empty

		Return False

	End Function

	Public Function GetAuthorizeUrl(ByRef reqToken As String) As Uri

		reqToken = GetRequestToken()

		Dim url As String = Twitter_AuthorizeUrl & "?oauth_token=" & reqToken

		Return New Uri(url)

	End Function

	Public Function GetRequestToken() As String
		Dim parameters As SortedDictionary(Of String, String) = GenerateOAuthParameters(String.Empty)
		Dim signature As String = GenerateSignature(String.Empty, "GET", Twitter_RequestTokenUrl, parameters)

		parameters.Add("oauth_signature", Http.UrlEncode(signature))

		Dim response As String = Http.Get(Twitter_RequestTokenUrl, parameters)
		Dim dic As Dictionary(Of String, String) = ParseResponse(response)

		Return dic("oauth_token")

	End Function

	Public Function Post(url As String, parameters As IEnumerable(Of KeyValuePair(Of String, String))) As DynamicResult
		Dim params As SortedDictionary(Of String, String) = GenerateOAuthParameters(m_Token)

		For Each p As KeyValuePair(Of String, String) In parameters
			params.Add(p.Key, p.Value)
		Next

		Dim signature As String = GenerateSignature(m_Secret, "POST", url, params)

		params.Add("oauth_signature", Http.UrlEncode(signature))

		Return Http.PostJson(url, params)

	End Function

	Public Function Request(uri As String, method As RequestMethod, parameters As IEnumerable(Of KeyValuePair(Of String, String))) As DynamicResult

		If method = RequestMethod.GET Then
			Return [Get](uri, parameters)
		ElseIf method = RequestMethod.POST Then
			Return Post(uri, parameters)
		Else
			Return New DynamicResult(Nothing)
		End If

	End Function

#End Region

#Region " Private methods and functions "

	Private Function GenerateNonce() As String

		Return Guid.NewGuid().ToString("N")

	End Function

	Private Function GenerateOAuthParameters(token As String) As SortedDictionary(Of String, String)
		Dim result As New SortedDictionary(Of String, String)

		result.Add("oauth_consumer_key", m_CK)
		result.Add("oauth_signature_method", "HMAC-SHA1")
		result.Add("oauth_timestamp", GenerateTimestamp())
		result.Add("oauth_nonce", GenerateNonce())
		result.Add("oauth_version", "1.0")
		If Not String.IsNullOrEmpty(token) Then
			result.Add("oauth_token", token)
		End If

		Return result

	End Function

	Private Function GenerateSignature(tokenSecret As String, httpMethod As String, url As String, parameters As IEnumerable(Of KeyValuePair(Of String, String))) As String
		Dim signatureBase As String = GenerateSignatureBase(httpMethod, url, parameters)

		Using hmacsha1 As New HMACSHA1
			hmacsha1.Key = Encoding.ASCII.GetBytes(Http.UrlEncode(ConsumerSecret) & AMP & Http.UrlEncode(tokenSecret))

			Return Convert.ToBase64String(hmacsha1.ComputeHash(System.Text.Encoding.ASCII.GetBytes(signatureBase)))
		End Using

	End Function

	Private Function GenerateSignatureBase(httpMethod As String, url As String, parameters As IEnumerable(Of KeyValuePair(Of String, String))) As String
		Dim result As New StringBuilder()

		result.Append(httpMethod)
		result.Append(AMP)
		result.Append(Http.UrlEncode(url))
		result.Append(AMP)
		result.Append(Http.UrlEncode(Http.JoinParameters(parameters)))

		Return result.ToString

	End Function

	Private Function GenerateTimestamp() As String
		Dim ts As TimeSpan = DateTime.UtcNow - New DateTime(1970, 1, 1, 0, 0, 0, 0)

		Return Convert.ToInt64(ts.TotalSeconds).ToString

	End Function

	Private Function ParseResponse(response As String) As Dictionary(Of String, String)
		Dim result As New Dictionary(Of String, String)

		For Each s As String In response.Split(CChar(AMP))
			Dim index As Integer = s.IndexOf(EQ)

			If index = -1 Then
				result.Add(s, String.Empty)
			Else
				result.Add(s.Substring(0, index), s.Substring(index + 1))
			End If
		Next

		Return result

	End Function

#End Region

#Region " Construction "

	Public Sub New()

		Me.New(Nothing, Nothing)

	End Sub

	''' <summary>
	''' 
	''' </summary>
	''' <param name="token"></param>
	''' <param name="secret"></param>
	Public Sub New(token As String, secret As String)

		m_Token = token
		m_Secret = secret
		m_UserID = 0
		m_ScreenName = String.Empty
		'If Not (token Is Nothing Or secret Is Nothing) Then
		'	Dim user As Object = GetOwnUser()

		'	m_UserID = user.Id
		'	m_ScreenName = user.ScreenName
		'End If

	End Sub

#End Region

End Class