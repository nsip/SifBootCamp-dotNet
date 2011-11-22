Option Compare Text
Option Explicit On 
Option Strict On

Imports System
Imports System.IO
Imports System.Collections
Imports System.Collections.Specialized
Imports Edustructures.Util
Imports Edustructures.SifWorks

Public Class AdkExamples
	Public Shared Reg As Boolean = True
	Public Shared Unreg As Boolean = False
    Public Shared Version As SifVersion
    Public Shared Args() As String

    Public Shared Function ParseCommandLine(ByVal agent As Agent, ByVal arguments As String()) As NameValueCollection
        If Args Is Nothing Then

            Args = arguments
            If arguments.Length > 0 AndAlso Not arguments(0).Chars(0) = "/"c Then
                Dim rsp As System.IO.FileInfo = New System.IO.FileInfo(arguments(0))
                If rsp.Exists Then
                    Try
                        Dim v As System.Collections.ArrayList = New System.Collections.ArrayList
                        Dim reader As System.IO.StreamReader = File.OpenText(rsp.FullName)

                        'Read properties in from a file
                        Try
                            Dim line As String = Nothing
                            line = reader.ReadLine()
                            While Not line Is Nothing
                                ' allow comment lines, starting with a ;
                                If (Not line.StartsWith(";")) Then
                                    For Each token As String In line.Split(" "c)
                                        v.Add(token)
                                    Next
                                End If
                                line = reader.ReadLine()

                            End While
                            reader.Close()

                        Finally
                            CType(reader, IDisposable).Dispose()
                        End Try


                        If v.Count > 0 Then
                            Args = New String(arguments.Length + v.Count - 1) {}
                            Array.Copy(arguments, 0, Args, 0, arguments.Length)
                            v.CopyTo(Args, arguments.Length)

                            System.Console.Out.Write("Reading command-line arguments from " + arguments(0) + ": ")
                            Dim arg As String
                            For Each arg In Args
                                System.Console.Out.Write(arg + " ")
                            Next

                            Console.WriteLine()
                            Console.WriteLine()
                        End If
                    Catch ex As System.Exception
                        Console.WriteLine("Error reading command-line arguments from agent.rsp file: " + ex.toString())
                    End Try
                End If
            End If
        End If

        If agent Is Nothing Then
            Dim l As Integer = 0
            While l < Args.Length
                If Args(l).Equals("/debug") Then
                    If l < Args.Length - 1 Then
                        Try
                            Adk.Debug = AdkDebugFlags.None
                            l = l + 1
                            Dim debugLevel As Integer = System.Int32.Parse(Args(l))
                            Select Case debugLevel
                                Case 1
                                    Adk.Debug = AdkDebugFlags.Minimal
                                Case 2
                                    Adk.Debug = AdkDebugFlags.Moderate
                                Case 3
                                    Adk.Debug = AdkDebugFlags.Detailed
                                Case 4
                                    Adk.Debug = AdkDebugFlags.Very_Detailed
                                Case 5
                                    Adk.Debug = AdkDebugFlags.All
                            End Select
                        Catch e As System.Exception
                            Adk.Debug = AdkDebugFlags.All
                        End Try
                    Else
                        Adk.Debug = AdkDebugFlags.All
                    End If
                Else
                    If Args(l).StartsWith("/D") Then
                        Dim prop As String = arguments(l).Substring(2)
                        If Not l = arguments.Length - 1 Then
                            l = l + 1
                            Properties.SetProperty(prop, Args(l))
                        Else
                            Console.WriteLine("Usage: /Dproperty value")
                        End If
                    Else
                        If Args(l).Equals("/log") AndAlso Not l = Args.Length - 1 Then
                            Try
                                l = l + 1
                                Adk.SetLogFile(Args(l))
                            Catch ioe As System.IO.IOException
                                Console.WriteLine("Could not redirect debug output to log file: " + ioe.ToString())
                            End Try
                        Else
                            If Args(l).Equals("/ver") AndAlso Not l = Args.Length - 1 Then
                                l = l + 1
                                Version = SifVersion.Parse(Args(l))
                            Else
                                If Args(l).Equals("/?") Then
                                    Console.WriteLine()
                                    Console.WriteLine("These options are common to all Adk Example agents. For help on the usage")
                                    Console.WriteLine("of this agent in particular, run the agent without any parameters. Note that")
                                    Console.WriteLine("most agents support multiple zones if a zones.properties file is found in")
                                    Console.WriteLine("the current directory.")
                                    Console.WriteLine()
                                    PrintHelp()
                                    System.Environment.Exit(0)
                                End If
                            End If
                        End If
                    End If
                End If
                l = l + 1
            End While
            Return Nothing
        End If

        Dim props As AgentProperties = agent.Properties
        Dim misc As NameValueCollection = New NameValueCollection
        Dim port As Integer = -1
        Dim host As String = Nothing
        Dim useHttps As Boolean = False
        Dim sslCert As String = Nothing
        Dim clientCert As String = Nothing
        Dim clientAuth As Int32 = 0
        Dim i As Integer = 0
        For i = 0 To Args.Length - 1
            Select Case Args(i)
                Case "/sourceId"
                    If Not i = Args.Length - 1 Then
                        i = i + 1
                        agent.Id = Args(i)
                    End If

                Case "/noreg"
                    Reg = False
                Case "/unreg"
                    Unreg = True
                Case "/pull"
                    props.MessagingMode = AgentMessagingMode.Pull
                Case "/push"
                    props.MessagingMode = AgentMessagingMode.Push
                Case "/port"
                    If Not i = Args.Length - 1 Then
                        Try
                            i = i + 1
                            port = System.Int32.Parse(Args(i))
                        Catch nfe As System.FormatException
                            Console.WriteLine("Invalid port: " + Args(i - 1))
                        End Try
                    End If
                Case "/https"
                    useHttps = True
                Case "/sslCert"
                    i = i + 1
                    sslCert = Args(i)
                Case "/clientCert"
                    i = i + 1
                    clientCert = Args(i)
                Case "/clientAuth"
                    i = i + 1
                    Try
                        clientAuth = Int32.Parse(Args(i))
                    Catch ex As Exception
                        clientAuth = 0
                    End Try
                Case "/host"
                    If Not i = Args.Length - 1 Then
                        i = i + 1
                        host = Args(i)
                    End If
                Case "/timeout"
                    If Not i = Args.Length - 1 Then
                        Try
                            i = i + 1
                            props.DefaultTimeout = TimeSpan.FromMilliseconds(System.Int32.Parse(Args(i)))
                        Catch nfe As System.FormatException
                            Dim ignore As String = nfe.ToString
                            Console.WriteLine("Invalid timeout: " + Args(i - 1))
                        End Try
                    End If
                Case "/freq"
                    If Not i = Args.Length - 1 Then
                        Try
                            i = i + 1
                            props.PullFrequency = TimeSpan.FromMilliseconds(Integer.Parse(Args(i)))
                        Catch nfe As System.FormatException
                            Dim ignore As String = nfe.ToString
                            Console.WriteLine("Invalid pull frequency: " + Args(i - 1))
                        End Try
                    End If
                Case "/opensif"
                    props.IgnoreProvisioningErrors = True
                Case Else
                    If Args(i).Chars(0) = "/"c Then
                        If i = Args.Length - 1 Or Args(i + 1).StartsWith("/") Then
                            misc(Args(i).Substring(1)) = Nothing
                        Else
                            i = i + 1
                            misc(Args(i - 1).Substring(1)) = Args(i)
                        End If
                    End If
            End Select
        Next
        If useHttps Then
            Dim https As HttpsProperties = agent.DefaultHttpsProperties
            If Not (sslCert Is Nothing) Then
                https.SSLCertName = sslCert
            End If
            If Not (clientCert Is Nothing) Then
                https.ClientCertName = clientCert
            End If
            https.ClientAuthLevel = clientAuth

            If port <> -1 Then
                https.Port = port
            End If
            https.Host = host
            props.TransportProtocol = "https"
        Else
            Dim http As HttpProperties = agent.DefaultHttpProperties
            If Not port = -1 Then
                http.Port = port
            End If
            http.Host = host
            props.TransportProtocol = "http"
        End If
        Return misc
    End Function

    Public Shared Sub PrintHelp()
        Console.WriteLine("    /sourceId name    The name of the agent")
        Console.WriteLine("    /ver version      Default SIF Version to use (e.g. 10r1, 10r2, etc.)")
        Console.WriteLine("    /debug level      Enable debugging to the console")
        Console.WriteLine("                         1 - Minimal")
        Console.WriteLine("                         2 - Moderate")
        Console.WriteLine("                         3 - Detailed")
        Console.WriteLine("                         4 - Very Detailed")
        Console.WriteLine("                         5 - All")
        Console.WriteLine("    /log file         Redirects logging to the specified file")
        Console.WriteLine("    /pull             Use Pull mode")
        Console.WriteLine("    /freq             Sets the Pull frequency (defaults to 15 seconds)")
        Console.WriteLine("    /push             Use Push mode")
        Console.WriteLine("    /port n           The local port for Push mode (defaults to 12000)")
        Console.WriteLine("    /host addr        The local IP address for push mode (defaults to any)")
        Console.WriteLine("    /noreg            Do not send a SIF_Register on startup (sent by default)")
        Console.WriteLine("    /unreg            Send a SIF_Unregister on exit (not sent by default)")
        Console.WriteLine("    /timeout ms       Sets the Adk timeout period (defaults to 30000)")
        Console.WriteLine("    /opensif          Ignores provisioning errors from OpenSIF")
        Console.WriteLine("    /Dproperty val    Sets a Java System property")
        Console.WriteLine()
        Console.WriteLine("  HTTPS Transport Options:")
        Console.WriteLine("  Certificates will be retrieved from the CurrentUser's Personal Store")
        Console.WriteLine("    /https            Use HTTPS instead of HTTP")
        Console.WriteLine("    /clientAuth       [1|2|3] The client authentication level to require")
        Console.WriteLine("    /sslCert          The subject of the certificate to use for ssl")
        Console.WriteLine("    /clientCert       The subject of the certificate to use for client authentication")
        Console.WriteLine()
        Console.WriteLine("  Response Files:")
        Console.WriteLine("    To use a response file instead of typing arguments on the command-line,")
        Console.WriteLine("    pass the name of the response file as the first argument. This text")
        Console.WriteLine("    file may contain any combination of arguments on one or more lines,")
        Console.WriteLine("    which are appended to any arguments specified on the command-line.")
        Console.WriteLine()
    End Sub

    Public Shared Function ReadZonesList() As NameValueCollection
        Dim list As NameValueCollection = New NameValueCollection
        Return list
    End Function

    Shared Sub New()
        Version = SifVersion.LATEST
    End Sub
End Class
