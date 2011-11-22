
''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
' 
' SIFWorks Agent Developer Kit (ADK) for .NET 
' Copyright ©2001-2008 Edustructures LLC 
' All rights reserved. 
'
''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

Imports System
Imports System.Collections.Specialized
Imports System.Reflection
Imports System.Text.RegularExpressions
Imports System.Xml.XPath
Imports Edustructures.SifWorks
Imports Edustructures.SifWorks.Infra
Imports Edustructures.SifWorks.Tools.XPath
Imports Edustructures.Util

Namespace SIFQuery

    Public Class SIFQuery
        Inherits Agent
        Implements IQueryResults

        Private Shared ReadOnly semaphore As New Object()
        Private Shared ReadOnly _agent As New SIFQuery()

        Public Sub New()

            MyBase.New("SIFQuery")
        End Sub
        ''' <summary> 
        ''' 
        ''' </summary> 
        ''' <param name="args"></param> 
        <STAThread()> _
        Public Shared Sub Main(ByVal args As String())

            Try
                If args.Length < 2 Then
                    Console.WriteLine("Usage: SIFQuery /zone zone /url url [/events] [options]")
                    Console.WriteLine(" /zone zone The name of the zone")
                    Console.WriteLine(" /url url The zone URL")
                    AdkExamples.PrintHelp()
                    Return
                End If

                ' Pre-parse the command-line before initializing the ADK 
                Adk.Debug = AdkDebugFlags.Moderate
                AdkExamples.ParseCommandLine(Nothing, args)

                ' Initialize the ADK with the specified version, loading only the Student SDO package 
                Adk.Initialize()

                ' Call StartAgent. 
                _agent.StartAgent(args)

                ' Turn down debugging 
                Adk.Debug = AdkDebugFlags.None

                ' Call runConsole() This method does not return until the agent shuts down 
                _agent.RunConsole()

                ' Wait for Ctrl-C to be pressed 
                Console.WriteLine("Agent is running (Press Ctrl-C to stop)")

                Dim adkWait As New AdkConsoleWait
                adkWait.WaitForExit()

            Catch e As Exception
                Console.WriteLine(e)
            Finally
                If _agent IsNot Nothing AndAlso _agent.Initialized Then
                    ' Always shutdown the agent on exit 
                    Try
                        _agent.Shutdown( ProvisioningFlags.None)
                    Catch adkEx As AdkException
                        Console.WriteLine(adkEx)
                    End Try
                End If
            End Try

        End Sub

        Private Sub StartAgent(ByVal args As String())
            Me.Initialize()
            Dim parameters As NameValueCollection = AdkExamples.ParseCommandLine(Me, args)

            Dim zoneId As String = DirectCast(parameters("zone"), String)
            Dim url As String = DirectCast(parameters("url"), String)

            If zoneId Is Nothing OrElse url Is Nothing Then
                Console.WriteLine("The /zone and /url parameters are required")
                Environment.[Exit](0)
            End If


            ' 1) Get an instance of the zone to connect to 
            Dim zone As IZone = ZoneFactory.GetInstance(zoneId, url)
            zone.SetQueryResults(Me)

            ' 2) Connect to zones 
            zone.Connect(ProvisioningFlags.Register)


        End Sub

        Private Sub RunConsole()
            Console.WriteLine("SIFQuery Command Line")
            Dim version As Version = Assembly.GetExecutingAssembly().GetName().Version
            Console.WriteLine("Version " + version.ToString(3))
            Console.WriteLine("Copyright " + CStr(DateTime.Now.Year) + ", Edustructures")

            PrintSQLHelp()
            Dim sqlPattern As New Regex("(?:select)(.*)(?:from)(.*)(?:where)(.*)$", RegexOptions.IgnoreCase)
            Dim finished As Boolean = False
            While Not finished
                PrintPrompt()
                Dim query As String = Console.ReadLine().Trim()

                If query.Length = 0 Then
                    Continue While
                End If

                Dim lcaseQuery As String = query.ToLower()
                If lcaseQuery.StartsWith("q") Then
                    finished = True
                    Continue While
                End If

                If lcaseQuery.IndexOf("where") = -1 Then
                    ' The regular expression requires a where clause 
                    query = query + " where "
                End If

                Dim results As Match = Nothing
                Try
                    results = sqlPattern.Match(query)
                Catch ex As Exception
                    Console.WriteLine("ERROR evaluating expression: " & ex.Message)
                    Continue While
                End Try

                If results.Captures.Count = 0 Then
                    Console.WriteLine("Unknown error evaluating expression.")
                    Continue While
                End If

                If results.Groups.Count >= 3 Then
                    Dim q As Query = CreateQuery(results.Groups(2).Value)
                    If q IsNot Nothing AndAlso AddConditions(q, results.Groups(3).Value) AndAlso AddSelectFields(q, results.Groups(1).Value) Then
                        Console.WriteLine("Sending Query to zone.... ")
                        Dim queryXML As String = q.ToXml(SifVersion.LATEST)
                        Console.WriteLine(queryXML)
                        ' Store the original source query in the userData property 
                        q.UserData = queryXML
                        Me.ZoneFactory.GetAllZones()(0).Query(q)
                    End If
                Else
                    Console.WriteLine("ERROR: Unrecognized query syntax...")
                    PrintSQLHelp()
                End If
            End While

        End Sub

        Private Sub PrintPrompt()
            Console.Write("SIF: ")
        End Sub

        Private Sub PrintSQLHelp()
            Console.WriteLine("Syntax: Select {fields} From {SIF Object} [Where {field}={value}] ")
            Console.WriteLine(" {fields} one or more field names, seperated by a comma")
            Console.WriteLine(" (may by empty or * )")
            Console.WriteLine(" {SIF Object} the name of a SIF Object that is provided in the zone")
            Console.WriteLine(" {field} a field name")
            Console.WriteLine(" {value} a value")
            Console.WriteLine("Examples:")
            Console.WriteLine("SIF: Select * from StudentPersonal")
            Console.WriteLine("SIF: Select * from StudentPersonal where RefId=43203167CFF14D08BB9C8E3FD0F9EC3C")
            Console.WriteLine("SIF: Select * from StudentPersonal where Name/FirstName=Amber")
            Console.WriteLine("SIF: Select Name/FirstName, Name/LastName from StudentPersonal where Demographics/Gender=F")
            Console.WriteLine("SIF: Select * from StudentSchoolEnrollment where RefId=43203167CFF14D08BB9C8E3FD0F9EC3C")
            Console.WriteLine()
        End Sub

        Private Function CreateQuery(ByVal fromClause As String) As Query
            Dim queryDef As IElementDef = Adk.Dtd.LookupElementDef(fromClause.Trim())
            If queryDef Is Nothing Then
                Console.WriteLine("ERROR: Unrecognized FROM statement: " + fromClause)
                PrintSQLHelp()
                Return Nothing
            Else
                Return New Query(queryDef)
            End If
        End Function

        Private Function AddSelectFields(ByVal q As Query, ByVal selectClause As String) As Boolean
            If selectClause.Length = 0 OrElse selectClause.IndexOf("*") > -1 Then
                Return True
            End If
            Dim fields As String() = selectClause.Split(New Char() {","c})
            For Each field As String In fields
                Dim val As String = field.Trim()
                If val.Length > 0 Then
                    Dim restriction As IElementDef = Adk.Dtd.LookupElementDefBySQP(q.ObjectType, val)
                    If restriction Is Nothing Then
                        Console.WriteLine("ERROR: Unrecognized SELECT field: " + val)
                        PrintSQLHelp()
                        Return False
                    Else
                        q.AddFieldRestriction(restriction)
                    End If
                End If
            Next
            Return True
        End Function

        Private Function AddConditions(ByVal q As Query, ByVal whereClause As String) As Boolean
            Dim added As Boolean = True
            whereClause = whereClause.Trim()
            If whereClause.Length = 0 Then
                Return added
            End If

            ' Currently, only support a very limited vocabulary (field=value) 
            Dim whereConditions As String() = Regex.Split(whereClause, "[aA][nN][dD]")
            If whereConditions.Length > 0 Then
                For Each condition As String In whereConditions
                    Dim fields As String() = condition.Trim().Split(New Char() {"="c})
                    If fields.Length <> 2 Then
                        Console.WriteLine("ERROR: Unsupported where clause: " + whereClause)
                        PrintSQLHelp()
                        added = False
                        Exit For
                    End If

                    Dim field As IElementDef = Adk.Dtd.LookupElementDefBySQP(q.ObjectType, fields(0))
                    If field Is Nothing Then
                        Console.WriteLine("ERROR: Unrecognized field in where clause: " + fields(0))
                        PrintSQLHelp()
                        added = False
                        Exit For
                    Else
                        q.AddCondition(field, ComparisonOperators.EQ, fields(1))
                    End If
                Next
            End If

            Return added
        End Function

        Public Sub OnQueryPending(ByVal info As IMessageInfo, ByVal zone As IZone) Implements Edustructures.SifWorks.IQueryResults.OnQueryPending
            Dim smi As SifMessageInfo = DirectCast(info, SifMessageInfo)
            Console.WriteLine("Sending SIF Request with MsgId " + smi.MsgId + " to zone " + zone.ZoneId)
        End Sub

        Public Sub OnQueryResults(ByVal data As IDataObjectInputStream, ByVal sifError As SIF_Error, ByVal zone As IZone, ByVal info As IMessageInfo) Implements Edustructures.SifWorks.IQueryResults.OnQueryResults

            Dim smi As SifMessageInfo = DirectCast(info, SifMessageInfo)
            Dim start As DateTime = DateTime.Now
            If smi.Timestamp.HasValue Then
                start = smi.Timestamp.Value
            End If

            Console.WriteLine()
            Console.WriteLine("********************************************* ")
            Console.WriteLine("Received SIF_Response packet from zone" + zone.ZoneId)
            Console.WriteLine("Details... ")
            Console.WriteLine("Request MsgId: " & smi.SIFRequestMsgId)
            Console.WriteLine("Packet Number: " & CStr(smi.PacketNumber))
            Console.WriteLine()

            If sifError IsNot Nothing Then

                Console.WriteLine("The publisher returned an error: ")
                Console.WriteLine("Category: " & CStr(sifError.SIF_Category) & " Code: " & CStr(sifError.SIF_Code))
                Console.WriteLine("Description " + sifError.SIF_Desc)
                If sifError.SIF_ExtendedDesc IsNot Nothing Then
                    Console.WriteLine("Details: " + sifError.SIF_ExtendedDesc)
                End If
                Return
            End If

            Try
                Dim objectCount As Integer = 0
                While data.Available
                    Dim [next] As SifDataObject = data.ReadDataObject()
                    objectCount += 1
                    Console.WriteLine()
                    Console.WriteLine("Text Values for " + [next].ElementDef.Name + " " + CStr(objectCount) + " {" + [next].Key + "}")

                    Dim context As SifXPathContext = SifXPathContext.NewSIFContext([next])

                    ' Print out all attributes 
                    Console.WriteLine("Attributes:")
                    Dim textNodes As XPathNodeIterator = context.[Select]("//@*")
                    While textNodes.MoveNext()
                        Dim navigator As XPathNavigator = textNodes.Current
                        Dim value As Element = DirectCast(navigator.UnderlyingObject, Element)
                        Dim valueDef As IElementDef = value.ElementDef
                        Console.WriteLine(valueDef.Parent.Tag(SifVersion.LATEST) + "/@" + valueDef.Tag(SifVersion.LATEST) + "=" + value.TextValue + ", ")
                    End While
                    Console.WriteLine()
                    ' Print out all elements that have a text value 
                    Console.WriteLine("Element:")
                    textNodes = context.[Select]("//*")
                    While textNodes.MoveNext()
                        Dim navigator As XPathNavigator = textNodes.Current
                        Dim value As Element = DirectCast(navigator.UnderlyingObject, Element)
                        Dim textValue As String = value.TextValue
                        If textValue IsNot Nothing Then
                            Dim valueDef As IElementDef = value.ElementDef
                            Console.WriteLine(valueDef.Tag(SifVersion.LATEST) + "=" + textValue + ", ")
                        End If

                    End While
                End While
                Console.WriteLine()



                Console.WriteLine("Total Objects in Packet: " + CStr(objectCount))
            Catch ex As Exception
                Console.WriteLine(ex.Message)
                Console.WriteLine(ex.StackTrace)
            End Try

            If Not smi.MorePackets Then
                ' This is the final packet. Print stats 
                Console.WriteLine("Final Packet has been received.")
                Dim ri As IRequestInfo = smi.SIFRequestInfo
                If ri IsNot Nothing Then
                    Console.WriteLine("Source Query: ")
                    Console.WriteLine(ri.UserData)
                    Dim difference As TimeSpan = start.Subtract(ri.RequestTime)
                    Console.WriteLine("Query execution time: " + CStr(difference.Milliseconds) + " ms")

                End If
            Else
                Console.WriteLine("This is not the final packet for this SIF_Response")
            End If

            Console.WriteLine("********************************************* ")
            Console.WriteLine()
            PrintPrompt()
        End Sub



    End Class

End Namespace