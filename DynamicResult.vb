Imports System.Dynamic

''' <summary>
''' 
''' </summary>
Public Class DynamicResult

#Region " Objects and variables "

	Private ReadOnly m_Element As XElement
	Private ReadOnly m_Type As JsonType

#End Region

#Region " Properties "

	''' <summary>
	''' Returns the <see cref="XElement">XML Element</see> that contains the values.
	''' </summary>
	''' <returns><see cref="XElement">XML Element</see></returns>
	Public ReadOnly Property Element As XElement
		Get
			Return m_Element
		End Get
	End Property

	''' <summary>
	''' Returns the Json data type of the object.
	''' </summary>
	''' <returns><see cref="JsonType" /></returns>
	Public ReadOnly Property JsonType As JsonType
		Get
			Return m_Type
		End Get
	End Property

#End Region

#Region " Public methods and functions "

	Public Function GetValue(Of T)() As T

		If Not m_Type = BlueHeron.DynamicJson.JsonType.null Then
			Dim tp As Type = GetType(T)
			Dim isSafe As Boolean

			Select Case tp
				Case Is = GetType(Boolean)
					If m_Type = BlueHeron.DynamicJson.JsonType.boolean Then
						isSafe = True
					End If
				Case Is = GetType(String), GetType(Char), GetType(DateTime)
					isSafe = True
				Case Is = GetType(Double), GetType(Long), GetType(Decimal), GetType(Integer), GetType(Single), GetType(Byte)
					If m_Type = BlueHeron.DynamicJson.JsonType.number Then
						isSafe = True
					End If
				Case Is = GetType(Array)
					If m_Type = BlueHeron.DynamicJson.JsonType.array Then
						'Return DirectCast(Convert.ChangeType(m_Element.Value, tp), T)
						Return DirectCast(Convert.ChangeType(New DynamicJson(m_Element, m_Type), tp), T)
					End If
				Case Is = GetType(Object)
					isSafe = True
			End Select

			If isSafe Then ' more or less
				Return DirectCast(Convert.ChangeType(m_Element.Value, tp), T)
			End If
		End If

		Return Nothing

	End Function

#End Region

#Region " Construction "

	''' <summary>
	''' Creates a new DynamicResult object, based on the given <see cref="XElement">XML Element</see>.
	''' </summary>
	''' <param name="element"><see cref="XElement" /></param>
	Public Sub New(element As XElement)

		m_Element = element
		If m_Element Is Nothing Then
			m_Element = New XElement(NULL, Nothing)
		Else
			Dim attType As XAttribute = m_Element.Attribute(TYPE)

			If Not attType Is Nothing Then
				If Not System.Enum.TryParse(Of JsonType)(element.Attribute(TYPE).Value, m_Type) Then
					m_Type = JsonType.null
				End If
			End If
		End If

	End Sub

#End Region

End Class