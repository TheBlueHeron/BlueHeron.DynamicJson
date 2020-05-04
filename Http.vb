Imports System.Dynamic
Imports System.IO
Imports System.Net
Imports System.Text

''' <summary>
''' Wrapper for <see cref="WebRequest">WebRequests</see>, that handles parameters safely.
''' </summary>
Public Class Http

#Region " Public methods and functions "

	Public Shared Function [Get](url As String, parameters As IEnumerable(Of KeyValuePair(Of String, String))) As String
		Dim result As String = String.Empty
		Dim req As WebRequest = WebRequest.Create(String.Format(fmt_Request, url, JoinParameters(parameters)))

		Using res As WebResponse = req.GetResponse()
			Using stream As Stream = res.GetResponseStream()
				Using reader As New StreamReader(stream)
					result = reader.ReadToEnd()
				End Using
			End Using
		End Using

		Return result

	End Function

	Public Shared Function GetJson(url As String, parameters As IEnumerable(Of KeyValuePair(Of String, String))) As DynamicResult
		Dim result As DynamicResult
		Dim req As WebRequest = WebRequest.Create(String.Format(fmt_Request, url, JoinParameters(parameters)))

		Using res As WebResponse = req.GetResponse()
			Using stream As Stream = res.GetResponseStream()
				Using reader As New StreamReader(stream)
					result = DynamicJson.Parse(stream)
				End Using
			End Using
		End Using

		Return result

	End Function

	Public Shared Function JoinParameters(parameters As IEnumerable(Of KeyValuePair(Of String, String))) As String
		Dim result As New StringBuilder

		For Each parameter As KeyValuePair(Of String, String) In parameters
			result.Append(parameter.Key)
			result.Append(EQ)
			result.Append(parameter.Value)
			result.Append(AMP)
		Next

		Return result.ToString.TrimEnd(CChar(AMP))

	End Function

	Public Shared Function Post(url As String, parameters As IEnumerable(Of KeyValuePair(Of String, String))) As String
		Dim data As Byte() = Encoding.ASCII.GetBytes(JoinParameters(parameters))
		Dim req As WebRequest = WebRequest.Create(url)
		Dim result As String = String.Empty

		req.Method = "POST"
		req.ContentType = "application/x-www-form-urlencoded"
		req.ContentLength = data.Length

		Using stream As Stream = req.GetRequestStream
			stream.Write(data, 0, data.Length)
		End Using
		Using res As WebResponse = req.GetResponse
			Using stream As Stream = res.GetResponseStream
				Using reader As New StreamReader(stream, Encoding.UTF8)
					result = reader.ReadToEnd
				End Using
			End Using
		End Using

		Return result

	End Function

	Public Shared Function PostJson(url As String, parameters As IEnumerable(Of KeyValuePair(Of String, String))) As DynamicResult
		Dim data As Byte() = Encoding.ASCII.GetBytes(JoinParameters(parameters))
		Dim req As WebRequest = WebRequest.Create(url)
		Dim result As DynamicResult

		req.Method = "POST"
		req.ContentType = "application/x-www-form-urlencoded"
		req.ContentLength = data.Length

		Using stream As Stream = req.GetRequestStream
			stream.Write(data, 0, data.Length)
		End Using
		Using res As WebResponse = req.GetResponse
			Using stream As Stream = res.GetResponseStream
				Using reader As New StreamReader(stream, Encoding.UTF8)
					result = DynamicJson.Parse(stream)
				End Using
			End Using
		End Using

		Return result

	End Function

	Public Shared Function UrlEncode(value As String, encoding As Encoding, Optional isUpper As Boolean = True) As String

		If String.IsNullOrEmpty(value.Trim) Then
			Return String.Empty
		End If

		Dim result As New StringBuilder
		Dim data As Byte() = encoding.GetBytes(value)

		For Each b As Byte In data
			If (b < 128) AndAlso (AllowedChars.IndexOf(ChrW(b)) <> -1) Then
				result.Append(ChrW(b))
			Else
				If isUpper Then
					result.Append(String.Format(fmt_NumUpper, CInt(b)))
				Else
					result.Append(String.Format(fmt_NumLower, CInt(b)))
				End If
			End If
		Next

		Return result.ToString

	End Function

	Public Shared Function UrlEncode(value As String) As String

		Return UrlEncode(value, Encoding.UTF8)

	End Function

#End Region

End Class