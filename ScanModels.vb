''' <summary>Kết quả 1 cổng mở, dùng để hiển thị trong ListView và xuất file.</summary>
Public Class OpenPortResult
    Public Property Host As String
    Public Property Port As Integer
    Public Property ServiceName As String
    Public Property ElapsedMs As Long
    Public Property Banner As String
    Public Property WebTitle As String

    Public Sub New(_host As String, _port As Integer, _service As String, _elapsed As Long)
        Host = _host
        Port = _port
        ServiceName = _service
        ElapsedMs = _elapsed
        Banner = ""
        WebTitle = ""
    End Sub
End Class
