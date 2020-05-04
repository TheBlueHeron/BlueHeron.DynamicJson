Imports System.Dynamic
Imports System.IO
Imports System.Reflection
Imports System.Runtime.Serialization.Json
Imports System.Text
Imports System.Xml

''' <summary>
''' 
''' </summary>
''' <remarks>Adapted from code by neuecc: http://dynamicjson.codeplex.com/ .</remarks>
Public Class DynamicJson
	Inherits DynamicObject

#Region " Objects and variables "

	Private ReadOnly m_Xml As XElement
	Private ReadOnly m_Type As JsonType

#End Region

#Region " Properties "

	Public ReadOnly Property IsArray As Boolean
		Get
			Return (m_Type = BlueHeron.DynamicJson.JsonType.array)
		End Get
	End Property

	Public ReadOnly Property IsDefined(name As String) As Boolean
		Get
			Return IsObject AndAlso (Not m_Xml.Element(name) Is Nothing)
		End Get
	End Property

	Public ReadOnly Property IsDefined(index As Integer) As Boolean
		Get
			Return IsArray AndAlso (Not m_Xml.Elements.ElementAtOrDefault(index) Is Nothing)
		End Get
	End Property

	Public ReadOnly Property IsObject As Boolean
		Get
			Return (m_Type = BlueHeron.DynamicJson.JsonType.object)
		End Get
	End Property

#End Region

#Region " Public methods and functions "

	''' <summary>Deletes the property with the given name.</summary>
	Public Function Delete(name As String) As Boolean
		Dim elem As XElement = m_Xml.Element(name)

		If elem Is Nothing Then
			Return False
		Else
			elem.Remove()
			Return True
		End If

	End Function

	''' <summary>Deletes the property at the given index.</summary>
	Public Function Delete(index As Integer) As Boolean
		Dim elem As XElement = m_Xml.Elements.ElementAtOrDefault(index)

		If elem Is Nothing Then
			Return False
		Else
			elem.Remove()
			Return True
		End If

	End Function

	''' <summary>Maps to Array or Class by Public PropertyName</summary>
	Public Function Deserialize(Of T)() As T

		Return CType(Deserialize(GetType(T)), T)

	End Function

	''' <summary>
	''' Returns the Json datatype of the given object.
	''' </summary>
	Public Shared Function GetJsonType(obj As Object) As JsonType

		If obj Is Nothing Then
			Return JsonType.null
		End If

		Select Case System.Type.GetTypeCode(obj.GetType)
			Case TypeCode.Boolean
				Return JsonType.boolean
			Case TypeCode.String, TypeCode.Char, TypeCode.DateTime
				Return JsonType.string
			Case TypeCode.Int16, TypeCode.Int32, TypeCode.Int64, TypeCode.UInt16, TypeCode.UInt32, TypeCode.UInt64, TypeCode.Single, TypeCode.Double, TypeCode.Decimal, TypeCode.SByte, TypeCode.Byte
				Return JsonType.number
			Case TypeCode.Object
				Return If(obj.GetType.IsArray, JsonType.array, JsonType.object)
			Case Else ' TypeCode.DBNull,TypeCode.Empty
				Return JsonType.null
		End Select

	End Function

	''' <summary>Converts a Json String to a <see cref="DynamicResult">DynamicResult</see> object, assuming the input is <see cref="Encoding.Unicode">Unicode</see>.</summary>
	Public Shared Function Parse(json As String) As DynamicResult

		Return Parse(json, Encoding.Unicode)

	End Function

	''' <summary>Converts a Json String to a <see cref="DynamicResult">DynamicResult</see> object, using the given <see cref="Encoding">Encoding</see>.</summary>
	Public Shared Function Parse(json As String, encoding As Encoding) As DynamicResult

		Using reader As XmlDictionaryReader = JsonReaderWriterFactory.CreateJsonReader(encoding.GetBytes(json), XmlDictionaryReaderQuotas.Max)
			Return New DynamicResult(XElement.Load(reader))
		End Using

	End Function

	''' <summary>Converts a <see cref="Stream">stream</see>, containing a Json String to a <see cref="DynamicResult">DynamicResult</see> object. The encoding is being auto-detected.</summary>
	Public Shared Function Parse(stream As Stream) As DynamicResult

		Using reader As XmlDictionaryReader = JsonReaderWriterFactory.CreateJsonReader(stream, XmlDictionaryReaderQuotas.Max)
			Return New DynamicResult(XElement.Load(reader))
		End Using

	End Function

	''' <summary>Converts a <see cref="Stream">stream</see>, containing a Json String to a <see cref="DynamicResult">DynamicResult</see> object using the given <see cref="Encoding">Encoding</see>.</summary>
	Public Shared Function Parse(stream As Stream, encoding As Encoding) As DynamicResult

		Using reader As XmlDictionaryReader = JsonReaderWriterFactory.CreateJsonReader(stream, encoding, XmlDictionaryReaderQuotas.Max, Sub(r)
																																			  End Sub)
			Return New DynamicResult(XElement.Load(reader))
		End Using

	End Function

	''' <summary>Creates a Json String from a primitive, an IEnumerable or an Object({public property name:property value})</summary>
	Public Shared Function Serialize(obj As Object) As String

		Return CreateJsonString(New XStreamingElement("root", CreateTypeAttr(GetJsonType(obj)), CreateJsonNode(obj)))

	End Function

#End Region

#Region " Private methods and functions "

	Private Shared Function CreateJsonNode(obj As Object) As Object
		Dim type As JsonType = GetJsonType(obj)

		Select Case type
			Case JsonType.string, JsonType.number
				Return obj
			Case JsonType.boolean
				Return obj.ToString.ToLower
			Case JsonType.object
				Return CreateXObject(obj)
			Case JsonType.array
				Return CreateXArray(CType(obj, IEnumerable))
			Case Else ' JsonType.@null:
				Return Nothing
		End Select

	End Function

	Private Shared Function CreateJsonString(element As XStreamingElement) As String

		Using ms As New MemoryStream
			Using writer As XmlDictionaryWriter = JsonReaderWriterFactory.CreateJsonWriter(ms, Encoding.Unicode)
				element.WriteTo(writer)
				writer.Flush()

				Return Encoding.Unicode.GetString(ms.ToArray)
			End Using
		End Using

	End Function

	Private Shared Function CreateTypeAttr(type As JsonType) As XAttribute

		Return New XAttribute(Constants.TYPE, type.ToString)

	End Function

	Private Shared Function CreateXArray(Of T As IEnumerable)(obj As T) As IEnumerable(Of XStreamingElement)

		Return obj.Cast(Of Object).Select(Function(o) New XStreamingElement(ITEM, CreateTypeAttr(GetJsonType(o)), CreateJsonNode(o)))

	End Function

	Private Shared Function CreateXObject(obj As Object) As IEnumerable(Of XStreamingElement)

		Return obj.GetType.GetProperties(BindingFlags.Public Or BindingFlags.Instance).Select(Function(pi) New With {.Name = pi.Name, .Value = pi.GetValue(obj, Nothing)}).Select(Function(a) New XStreamingElement(a.Name, CreateTypeAttr(GetJsonType(a.Value)), CreateJsonNode(a.Value)))

	End Function

	Private Function Deserialize(type As Type) As Object

		'Return If(IsArray, DeserializeArray(type), DeserializeObject(type))

	End Function

	Private Function DeserializeObject(targetType As Type) As Object
		Dim result As Object = Activator.CreateInstance(targetType)
		Dim dict As Dictionary(Of String, PropertyInfo) = targetType.GetProperties(BindingFlags.Public Or BindingFlags.Instance).Where(Function(p) p.CanWrite).ToDictionary(Function(pi) pi.Name, Function(pi) pi)

		For Each item As XElement In m_Xml.Elements
			Dim propertyInfo As PropertyInfo

			If Not dict.TryGetValue(item.Name.LocalName, propertyInfo) Then
				Continue For
			End If

			'Dim value = DeserializeValue(item, propertyInfo.PropertyType)

			'propertyInfo.SetValue(result, value, Nothing)
		Next

		Return result

	End Function

#End Region

#Region " Construction "

	Public Sub New()

		m_Xml = New XElement(ROOT, CreateTypeAttr(JsonType.object))
		m_Type = JsonType.object

	End Sub

	Friend Sub New(element As XElement, type As JsonType)

		m_Xml = element
		m_Type = type

	End Sub

#End Region

End Class