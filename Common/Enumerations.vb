
''' <summary>
''' Enumeration of possible Json data types.
''' </summary>
Public Enum JsonType
	[string]
	number
	[boolean]
	[object]
	[array]
	null
End Enum

''' <summary>
''' Enumeration of possible types of web request.
''' </summary>
Public Enum RequestMethod
	[GET]
	POST
End Enum