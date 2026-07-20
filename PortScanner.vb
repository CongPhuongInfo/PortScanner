Imports System
Imports System.IO
Imports System.Linq
Imports System.Net
Imports System.Net.Sockets
Imports System.Net.NetworkInformation
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading
Imports System.Collections.Generic
Imports System.Collections.Concurrent
Imports System.Diagnostics

''' <summary>
''' Lớp quét cổng TCP đa luồng, hỗ trợ nhiều host/dải IP cùng lúc.
''' Hàng đợi công việc dùng ConcurrentQueue(Of Tuple(Host, Port)) để nhiều luồng lấy việc an toàn.
''' Có thể ping trước để báo trạng thái host (không chặn quét, vì ICMP có thể bị chặn dù cổng vẫn mở).
''' </summary>
Public Class PortScanner

    Public Property hosts As List(Of String)
    Public Property min As Integer
    Public Property max As Integer
    Public Property timeout As Integer
    Public Property Threads As Integer
    Public Property PingBeforeScan As Boolean = True

    ''' <summary>Nếu được gán (preset "cổng phổ biến"), quét đúng danh sách này thay vì min..max.</summary>
    Public Property PortsList As List(Of Integer) = Nothing

    Private workQueue As New ConcurrentQueue(Of Tuple(Of String, Integer))
    Private stopRequested As Boolean = False
    Private activeThreads As Integer = 0
    Private ReadOnly counterLock As New Object()

    Public Event HostPingResult(host As String, isAlive As Boolean, roundtripMs As Long)
    Public Event PortProcessing(host As String, port As Integer)
    Public Event PortOpen(host As String, port As Integer, serviceName As String, elapsedMs As Long)
    Public Event BannerReceived(host As String, port As Integer, banner As String)
    Public Event BannerError(host As String, port As Integer, message As String)
    Public Event WebTitleFound(host As String, port As Integer, title As String, url As String)
    Public Event WebTitleNotFound(host As String, port As Integer, url As String)
    Public Event ScanCompleted()

    Public Sub New(_hosts As List(Of String),
                   Optional _min As Integer = 1,
                   Optional _max As Integer = 65535,
                   Optional _timeout As Integer = 500,
                   Optional _threads As Integer = 20,
                   Optional _pingFirst As Boolean = True)
        Me.hosts = _hosts
        Me.min = _min
        Me.max = _max
        Me.timeout = _timeout
        Me.Threads = If(_threads < 1, 1, _threads)
        Me.PingBeforeScan = _pingFirst
    End Sub

    ''' <summary>Tổng số cặp (host, port) sẽ được quét - dùng để hiển thị thanh tiến trình.</summary>
    Public Function GetTotalWorkItems() As Long
        Dim portCount As Long
        If PortsList IsNot Nothing AndAlso PortsList.Count > 0 Then
            portCount = PortsList.Count
        Else
            portCount = CLng(max) - CLng(min) + 1L
        End If
        Return portCount * CLng(hosts.Count)
    End Function

    Public Sub start()
        stopRequested = False
        workQueue = New ConcurrentQueue(Of Tuple(Of String, Integer))

        Dim starterThread As New Thread(AddressOf PrepareAndRun)
        starterThread.IsBackground = True
        starterThread.Start()
    End Sub

    Public Sub RequestStop()
        stopRequested = True
    End Sub

    Private Sub PrepareAndRun()
        ' Ping trước để báo trạng thái (không chặn quét cổng dù host không phản hồi ICMP)
        If PingBeforeScan Then
            For Each h As String In hosts
                If stopRequested Then Exit For
                Try
                    Using p As New Ping()
                        Dim reply As PingReply = p.Send(h, Math.Min(timeout, 2000))
                        Dim alive As Boolean = (reply.Status = IPStatus.Success)
                        RaiseEvent HostPingResult(h, alive, If(alive, reply.RoundtripTime, -1L))
                    End Using
                Catch ex As Exception
                    RaiseEvent HostPingResult(h, False, -1L)
                End Try
            Next
        End If

        If stopRequested Then
            RaiseEvent ScanCompleted()
            Return
        End If

        Dim portsToScan As IEnumerable(Of Integer)
        If PortsList IsNot Nothing AndAlso PortsList.Count > 0 Then
            portsToScan = PortsList
        Else
            portsToScan = Enumerable.Range(min, max - min + 1)
        End If

        For Each h As String In hosts
            For Each portNum As Integer In portsToScan
                workQueue.Enqueue(New Tuple(Of String, Integer)(h, portNum))
            Next
        Next

        activeThreads = Threads
        For i As Integer = 0 To Threads - 1
            Dim th As New Thread(New ThreadStart(AddressOf runscan))
            th.IsBackground = True
            th.Start()
        Next
    End Sub

    Private Sub runscan()
        Dim item As Tuple(Of String, Integer) = Nothing

        While (Not stopRequested) AndAlso workQueue.TryDequeue(item)
            Dim h As String = item.Item1
            Dim port As Integer = item.Item2

            RaiseEvent PortProcessing(h, port)

            Dim isOpen As Boolean = False
            Dim elapsedMs As Long = 0
            Dim sw As Stopwatch = Stopwatch.StartNew()
            Try
                CheckPort(h, port, timeout)
                isOpen = True
            Catch
                isOpen = False
            End Try
            sw.Stop()
            elapsedMs = sw.ElapsedMilliseconds

            If isOpen Then
                Dim serviceName As String = ServicePorts.GetServiceName(port)
                RaiseEvent PortOpen(h, port, serviceName, elapsedMs)

                Try
                    Dim banner As String = BannerGrab(h, port, timeout)
                    RaiseEvent BannerReceived(h, port, banner)
                Catch ex As Exception
                    RaiseEvent BannerError(h, port, ex.Message)
                End Try

                Dim url As String = "http://" & h & ":" & port.ToString()
                Dim webpageTitle As String = GetPageTitle(url)
                If String.IsNullOrWhiteSpace(webpageTitle) = False Then
                    RaiseEvent WebTitleFound(h, port, webpageTitle, url)
                Else
                    RaiseEvent WebTitleNotFound(h, port, url)
                End If
            End If
        End While

        Dim isLast As Boolean = False
        SyncLock counterLock
            activeThreads -= 1
            isLast = (activeThreads <= 0)
        End SyncLock

        If isLast Then
            RaiseEvent ScanCompleted()
        End If
    End Sub

    Private Sub CheckPort(hostName As String, portNumber As Integer, timeoutMs As Integer)
        Using tcp As New TcpClient()
            Dim ar As IAsyncResult = tcp.BeginConnect(hostName, portNumber, Nothing, Nothing)
            Dim connected As Boolean = ar.AsyncWaitHandle.WaitOne(timeoutMs, False)
            If Not connected OrElse Not tcp.Connected Then
                Try
                    tcp.Close()
                Catch
                End Try
                Throw New Exception("Không mở hoặc quá thời gian chờ")
            End If
            tcp.EndConnect(ar)
        End Using
    End Sub

    Private Function GetPageTitle(ip As String) As String
        Try
            Dim wc As New WebClient()
            wc.Headers.Add("User-Agent", "Mozilla/5.0")
            Dim str As String = wc.DownloadString(ip)
            Dim result As String = Regex.Match(str, "\<title\b[^>]*\>\s*(?<Title>[\s\S]*?)\</title\>", RegexOptions.IgnoreCase).Groups("Title").Value
            Return result.Trim()
        Catch ex As Exception
            Return ""
        End Try
    End Function

    Public Function BannerGrab(ByVal hostName As String, ByVal portNumber As Integer, ByVal timeoutMs As Integer) As String
        Using newClient As New TcpClient()
            Dim ar As IAsyncResult = newClient.BeginConnect(hostName, portNumber, Nothing, Nothing)
            If Not ar.AsyncWaitHandle.WaitOne(timeoutMs, False) Then
                Throw New Exception("Quá thời gian chờ khi lấy banner")
            End If
            newClient.EndConnect(ar)
            newClient.SendTimeout = timeoutMs
            newClient.ReceiveTimeout = timeoutMs

            Using ns As NetworkStream = newClient.GetStream()
                Dim request As String = "HEAD / HTTP/1.1" & vbCrLf &
                                         "Host: " & hostName & vbCrLf &
                                         "Connection: Close" & vbCrLf & vbCrLf
                Dim requestBytes() As Byte = Encoding.ASCII.GetBytes(request)
                ns.Write(requestBytes, 0, requestBytes.Length)
                ns.Flush()

                Dim bytes(2047) As Byte
                Dim bytesRead As Integer = ns.Read(bytes, 0, bytes.Length)
                Dim response As String = Encoding.ASCII.GetString(bytes, 0, bytesRead)
                Return response
            End Using
        End Using
    End Function

End Class
