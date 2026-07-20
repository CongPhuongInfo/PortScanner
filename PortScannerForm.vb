Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Threading
Imports System.Net
Imports System.Collections.Generic
Imports System.IO
Imports System.Text

Public Class PortScannerForm
    Inherits Form

    Private WithEvents txtHosts As TextBox
    Private WithEvents cboPreset As ComboBox
    Private WithEvents numMin As NumericUpDown
    Private WithEvents numMax As NumericUpDown
    Private WithEvents numTimeout As NumericUpDown
    Private WithEvents numThreads As NumericUpDown
    Private WithEvents chkPing As CheckBox
    Private WithEvents btnStart As Button
    Private WithEvents btnStop As Button
    Private WithEvents btnExport As Button
    Private lblHostsHint As Label
    Private lblStatus As Label
    Private progress As ProgressBar
    Private rtbLog As RichTextBox
    Private lvOpenPorts As ListView

    Private scanner As PortScanner
    Private scannedCount As Long = 0
    Private ReadOnly progressLock As New Object()
    Private results As New List(Of OpenPortResult)
    Private ReadOnly resultsLock As New Object()

    Public Sub New()
        InitializeComponent()
    End Sub

    Private Sub InitializeComponent()
        Me.AutoScaleMode = AutoScaleMode.Font
        Me.Text = "Trình Quét Cổng TCP"
        Me.Width = 900
        Me.Height = 700
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.MinimumSize = New Size(760, 560)
        Me.Font = New Font("Segoe UI", 9.0F)

        Dim lblHost As New Label() With {.Text = "Host/IP (nhiều host cách nhau bởi dấu phẩy):", .Left = 12, .Top = 13, .Width = 420, .Height = 18, .AutoSize = False}
        txtHosts = New TextBox() With {.Text = "127.0.0.1", .Left = 12, .Top = 33, .Width = 420}
        lblHostsHint = New Label() With {
            .Text = "VD: 127.0.0.1, 192.168.1.1-192.168.1.20, example.com",
            .Left = 12, .Top = 59, .Width = 420, .Height = 16, .AutoSize = False,
            .ForeColor = Color.Gray, .Font = New Font("Segoe UI", 7.5F)
        }

        Dim lblPreset As New Label() With {.Text = "Dải cổng:", .Left = 450, .Top = 13, .Width = 150, .Height = 18, .AutoSize = False}
        cboPreset = New ComboBox() With {.Left = 450, .Top = 33, .Width = 260, .DropDownStyle = ComboBoxStyle.DropDownList}
        cboPreset.Items.AddRange(New String() {
            "Tùy chỉnh (theo cổng bắt đầu/kết thúc)",
            "Cổng phổ biến (~90 cổng thường gặp)",
            "Well-known (1 - 1024)",
            "Toàn bộ (1 - 65535)"
        })

        Dim lblMin As New Label() With {.Text = "Cổng bắt đầu:", .Left = 12, .Top = 90, .Width = 100, .Height = 18, .AutoSize = False}
        numMin = New NumericUpDown() With {.Minimum = 1, .Maximum = 65535, .Value = 1, .Left = 120, .Top = 87, .Width = 90}

        Dim lblMax As New Label() With {.Text = "Cổng kết thúc:", .Left = 220, .Top = 90, .Width = 100, .Height = 18, .AutoSize = False}
        numMax = New NumericUpDown() With {.Minimum = 1, .Maximum = 65535, .Value = 1024, .Left = 330, .Top = 87, .Width = 90}

        Dim lblTimeout As New Label() With {.Text = "Timeout (ms):", .Left = 450, .Top = 90, .Width = 100, .Height = 18, .AutoSize = False}
        numTimeout = New NumericUpDown() With {.Minimum = 50, .Maximum = 60000, .Value = 500, .Left = 560, .Top = 87, .Width = 90}

        Dim lblThreads As New Label() With {.Text = "Số luồng:", .Left = 670, .Top = 90, .Width = 70, .Height = 18, .AutoSize = False}
        numThreads = New NumericUpDown() With {.Minimum = 1, .Maximum = 500, .Value = 20, .Left = 745, .Top = 87, .Width = 90}

        chkPing = New CheckBox() With {.Text = "Ping kiểm tra host trước khi quét", .Left = 12, .Top = 122, .Width = 300, .Height = 24, .Checked = True}

        ' Dời 2 nút xuống cùng hàng với checkbox Ping (bên phải, không còn cùng hàng
        ' với ComboBox "Dải cổng" nữa) để tránh bị chồng lên nhau.
        btnStart = New Button() With {.Text = "Bắt đầu quét", .Left = 620, .Top = 118, .Width = 120, .Height = 32}
        btnStop = New Button() With {.Text = "Dừng", .Left = 750, .Top = 118, .Width = 85, .Height = 32, .Enabled = False}

        lblStatus = New Label() With {.Text = "Sẵn sàng.", .Left = 12, .Top = 158, .Width = 840, .Height = 20}
        progress = New ProgressBar() With {.Left = 12, .Top = 181, .Width = 840, .Height = 18, .Minimum = 0}

        lvOpenPorts = New ListView() With {
            .Left = 12, .Top = 207, .Width = 840, .Height = 180,
            .Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right,
            .View = View.Details, .FullRowSelect = True, .GridLines = True
        }
        lvOpenPorts.Columns.Add("Host", 150)
        lvOpenPorts.Columns.Add("Cổng", 60)
        lvOpenPorts.Columns.Add("Dịch vụ", 150)
        lvOpenPorts.Columns.Add("Phản hồi (ms)", 100)
        lvOpenPorts.Columns.Add("Tiêu đề Web", 190)
        lvOpenPorts.Columns.Add("Banner (rút gọn)", 180)

        btnExport = New Button() With {.Text = "Xuất kết quả (CSV/TXT)", .Left = 12, .Top = 393, .Width = 190, .Height = 30, .Enabled = False}

        rtbLog = New RichTextBox() With {
            .Left = 12, .Top = 433, .Width = 840, .Height = 220,
            .Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right,
            .ReadOnly = True,
            .BackColor = Color.Black,
            .ForeColor = Color.LightGray,
            .Font = New Font("Consolas", 9.5F)
        }

        Me.Controls.AddRange(New Control() {
            lblHost, txtHosts, lblHostsHint, lblPreset, cboPreset,
            lblMin, numMin, lblMax, numMax,
            lblTimeout, numTimeout, lblThreads, numThreads, chkPing,
            btnStart, btnStop, lblStatus, progress, lvOpenPorts, btnExport, rtbLog
        })

        ' Đặt SelectedIndex sau cùng, vì việc set giá trị này sẽ kích hoạt ngay
        ' sự kiện cboPreset_SelectedIndexChanged, và sự kiện đó cần numMin/numMax
        ' đã tồn tại (nếu đặt sớm hơn lúc numMin/numMax chưa được New thì sẽ
        ' NullReferenceException ngay khi form đang dựng giao diện).
        cboPreset.SelectedIndex = 0
    End Sub

    Private Sub cboPreset_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cboPreset.SelectedIndexChanged
        Select Case cboPreset.SelectedIndex
            Case 0 ' Tùy chỉnh
                numMin.Enabled = True
                numMax.Enabled = True
            Case 1 ' Cổng phổ biến
                numMin.Enabled = False
                numMax.Enabled = False
            Case 2 ' Well-known
                numMin.Enabled = False
                numMax.Enabled = False
                numMin.Value = 1
                numMax.Value = 1024
            Case 3 ' Toàn bộ
                numMin.Enabled = False
                numMax.Enabled = False
                numMin.Value = 1
                numMax.Value = 65535
        End Select
    End Sub

    Private Sub btnStart_Click(sender As Object, e As EventArgs) Handles btnStart.Click
        Dim hostList As List(Of String) = ParseHosts(txtHosts.Text)
        If hostList.Count = 0 Then
            MessageBox.Show("Vui lòng nhập ít nhất một Host/IP hợp lệ.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim presetPorts As List(Of Integer) = Nothing
        If cboPreset.SelectedIndex = 1 Then
            presetPorts = New List(Of Integer)(ServicePorts.TopCommonPorts)
        ElseIf numMin.Value > numMax.Value Then
            MessageBox.Show("Cổng bắt đầu phải nhỏ hơn hoặc bằng cổng kết thúc.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        rtbLog.Clear()
        lvOpenPorts.Items.Clear()
        SyncLock resultsLock
            results.Clear()
        End SyncLock
        btnExport.Enabled = False
        scannedCount = 0

        scanner = New PortScanner(hostList, CInt(numMin.Value), CInt(numMax.Value), CInt(numTimeout.Value), CInt(numThreads.Value), chkPing.Checked)
        scanner.PortsList = presetPorts

        progress.Maximum = CInt(Math.Min(scanner.GetTotalWorkItems(), Integer.MaxValue))
        progress.Value = 0

        AddHandler scanner.HostPingResult, AddressOf OnHostPingResult
        AddHandler scanner.PortProcessing, AddressOf OnPortProcessing
        AddHandler scanner.PortOpen, AddressOf OnPortOpen
        AddHandler scanner.BannerReceived, AddressOf OnBannerReceived
        AddHandler scanner.BannerError, AddressOf OnBannerError
        AddHandler scanner.WebTitleFound, AddressOf OnWebTitleFound
        AddHandler scanner.WebTitleNotFound, AddressOf OnWebTitleNotFound
        AddHandler scanner.ScanCompleted, AddressOf OnScanCompleted

        SetUiScanning(True)
        AppendLog("Bắt đầu quét " & hostList.Count.ToString() & " host: " & String.Join(", ", hostList.ToArray()), Color.White)
        scanner.start()
    End Sub

    Private Sub btnStop_Click(sender As Object, e As EventArgs) Handles btnStop.Click
        If scanner IsNot Nothing Then
            scanner.RequestStop()
            AppendLog("Đã yêu cầu dừng, đang chờ các luồng kết thúc...", Color.Orange)
            btnStop.Enabled = False
        End If
    End Sub

    Private Sub btnExport_Click(sender As Object, e As EventArgs) Handles btnExport.Click
        Using sfd As New SaveFileDialog()
            sfd.Filter = "CSV (*.csv)|*.csv|Text (*.txt)|*.txt"
            sfd.FileName = "PortScanResult.csv"
            If sfd.ShowDialog(Me) = DialogResult.OK Then
                Try
                    If sfd.FilterIndex = 1 Then
                        ExportCsv(sfd.FileName)
                    Else
                        ExportTxt(sfd.FileName)
                    End If
                    MessageBox.Show("Đã xuất kết quả ra: " & sfd.FileName, "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Catch ex As Exception
                    MessageBox.Show("Lỗi khi xuất file: " & ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
            End If
        End Using
    End Sub

    Private Sub ExportCsv(path As String)
        Dim sb As New StringBuilder()
        sb.AppendLine("Host,Port,Service,ElapsedMs,WebTitle,Banner")
        SyncLock resultsLock
            For Each r As OpenPortResult In results
                sb.AppendLine(CsvEscape(r.Host) & "," & r.Port.ToString() & "," & CsvEscape(r.ServiceName) & "," &
                              r.ElapsedMs.ToString() & "," & CsvEscape(r.WebTitle) & "," & CsvEscape(r.Banner))
            Next
        End SyncLock
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8)
    End Sub

    Private Sub ExportTxt(path As String)
        File.WriteAllText(path, rtbLog.Text, Encoding.UTF8)
    End Sub

    Private Function CsvEscape(value As String) As String
        If value Is Nothing Then Return ""
        Dim v As String = value.Replace(Environment.NewLine, " ").Replace(vbCr, " ").Replace(vbLf, " ")
        If v.Contains(",") OrElse v.Contains("""") Then
            v = """" & v.Replace("""", """""") & """"
        End If
        Return v
    End Function

    ' ==== Phân tích chuỗi Host nhập vào: hỗ trợ danh sách phân cách bởi dấu phẩy và dải IP a.b.c.d-e ====

    Private Function ParseHosts(input As String) As List(Of String)
        Dim result As New List(Of String)
        If String.IsNullOrWhiteSpace(input) Then Return result

        Dim parts() As String = input.Split(New Char() {","c, ";"c}, StringSplitOptions.RemoveEmptyEntries)
        For Each raw As String In parts
            Dim token As String = raw.Trim()
            If token.Length = 0 Then Continue For

            If token.Contains("-") Then
                Dim dashIndex As Integer = token.LastIndexOf("-"c)
                Dim startText As String = token.Substring(0, dashIndex).Trim()
                Dim endText As String = token.Substring(dashIndex + 1).Trim()

                Dim startIp As IPAddress = Nothing
                If IPAddress.TryParse(startText, startIp) Then
                    ' Hỗ trợ dạng rút gọn: 192.168.1.1-50 (chỉ đổi octet cuối)
                    If Not endText.Contains(".") Then
                        Dim lastDot As Integer = startText.LastIndexOf("."c)
                        If lastDot > 0 Then
                            endText = startText.Substring(0, lastDot + 1) & endText
                        End If
                    End If

                    Dim endIp As IPAddress = Nothing
                    If IPAddress.TryParse(endText, endIp) Then
                        result.AddRange(ExpandIpRange(startIp, endIp))
                        Continue For
                    End If
                End If
            End If

            result.Add(token)
        Next
        Return result
    End Function

    Private Function ExpandIpRange(startIp As IPAddress, endIp As IPAddress) As List(Of String)
        Dim list As New List(Of String)
        Dim startBytes() As Byte = startIp.GetAddressBytes()
        Dim endBytes() As Byte = endIp.GetAddressBytes()
        If startBytes.Length <> 4 OrElse endBytes.Length <> 4 Then
            list.Add(startIp.ToString())
            Return list
        End If

        Dim startVal As UInteger = (CUInt(startBytes(0)) << 24) Or (CUInt(startBytes(1)) << 16) Or (CUInt(startBytes(2)) << 8) Or CUInt(startBytes(3))
        Dim endVal As UInteger = (CUInt(endBytes(0)) << 24) Or (CUInt(endBytes(1)) << 16) Or (CUInt(endBytes(2)) << 8) Or CUInt(endBytes(3))

        If endVal < startVal Then
            Dim tmp As UInteger = startVal
            startVal = endVal
            endVal = tmp
        End If

        ' Giới hạn an toàn: tối đa 1024 địa chỉ trong 1 dải để tránh treo máy do nhập nhầm
        Const maxCount As UInteger = 1024UI
        Dim count As UInteger = 0
        Dim v As UInteger = startVal
        While v <= endVal AndAlso count < maxCount
            Dim b0 As Byte = CByte((v >> 24) And &HFFUI)
            Dim b1 As Byte = CByte((v >> 16) And &HFFUI)
            Dim b2 As Byte = CByte((v >> 8) And &HFFUI)
            Dim b3 As Byte = CByte(v And &HFFUI)
            list.Add(b0.ToString() & "." & b1.ToString() & "." & b2.ToString() & "." & b3.ToString())
            v += 1UI
            count += 1UI
        End While

        Return list
    End Function

    ' ==== Các sự kiện từ PortScanner (chạy trên luồng nền => phải Invoke về UI thread) ====

    Private Sub OnHostPingResult(host As String, isAlive As Boolean, roundtripMs As Long)
        If isAlive Then
            AppendLog("[PING] " & host & " đang hoạt động (" & roundtripMs.ToString() & " ms)", Color.Cyan)
        Else
            AppendLog("[PING] " & host & " không phản hồi ICMP (vẫn tiếp tục quét cổng, có thể do tường lửa chặn ping)", Color.DarkOrange)
        End If
    End Sub

    Private Sub OnPortProcessing(host As String, port As Integer)
        SyncLock progressLock
            scannedCount += 1
        End SyncLock
        RunOnUi(Sub()
                    lblStatus.Text = "Đang xử lý: " & host & ":" & port.ToString()
                    If scannedCount <= progress.Maximum Then progress.Value = CInt(scannedCount)
                End Sub)
    End Sub

    Private Sub OnPortOpen(host As String, port As Integer, serviceName As String, elapsedMs As Long)
        AppendLog("Cổng TCP " & host & ":" & port & " đang mở [" & serviceName & "] (" & elapsedMs & " ms)", Color.LightGreen)

        Dim r As New OpenPortResult(host, port, serviceName, elapsedMs)
        SyncLock resultsLock
            results.Add(r)
        End SyncLock

        RunOnUi(Sub()
                    Dim item As New ListViewItem(New String() {host, port.ToString(), serviceName, elapsedMs.ToString(), "", ""})
                    item.Tag = r
                    lvOpenPorts.Items.Add(item)
                    btnExport.Enabled = True
                End Sub)
    End Sub

    Private Sub OnBannerReceived(host As String, port As Integer, banner As String)
        Dim shortBanner As String = banner.Replace(vbCrLf, " | ").Trim()
        If shortBanner.Length > 120 Then shortBanner = shortBanner.Substring(0, 120) & "..."
        AppendLog("[" & host & ":" & port & "] Banner: " & shortBanner, Color.Khaki)
        UpdateResultField(host, port, Sub(r) r.Banner = banner, 5, shortBanner)
    End Sub

    Private Sub OnBannerError(host As String, port As Integer, message As String)
        AppendLog("[" & host & ":" & port & "] Không thể lấy banner :: " & message, Color.IndianRed)
    End Sub

    Private Sub OnWebTitleFound(host As String, port As Integer, title As String, url As String)
        AppendLog("Webpage Title = """ & title & """ tại " & url, Color.LightGreen)
        UpdateResultField(host, port, Sub(r) r.WebTitle = title, 4, title)
    End Sub

    Private Sub OnWebTitleNotFound(host As String, port As Integer, url As String)
        AppendLog("Có thể có dịch vụ khác @ " & url, Color.MediumPurple)
    End Sub

    Private Sub UpdateResultField(host As String, port As Integer, apply As Action(Of OpenPortResult), columnIndex As Integer, displayValue As String)
        SyncLock resultsLock
            For Each r As OpenPortResult In results
                If r.Host = host AndAlso r.Port = port Then
                    apply(r)
                    Exit For
                End If
            Next
        End SyncLock

        RunOnUi(Sub()
                    For Each it As ListViewItem In lvOpenPorts.Items
                        Dim tagResult As OpenPortResult = TryCast(it.Tag, OpenPortResult)
                        If tagResult IsNot Nothing AndAlso tagResult.Host = host AndAlso tagResult.Port = port Then
                            it.SubItems(columnIndex).Text = displayValue
                            Exit For
                        End If
                    Next
                End Sub)
    End Sub

    Private Sub OnScanCompleted()
        RunOnUi(Sub()
                    AppendLog("Đã quét xong !!!", Color.White)
                    lblStatus.Text = "Hoàn tất."
                    progress.Value = progress.Maximum
                    SetUiScanning(False)
                End Sub)
    End Sub

    ' ==== Tiện ích ====

    Private Sub SetUiScanning(isScanning As Boolean)
        btnStart.Enabled = Not isScanning
        btnStop.Enabled = isScanning
        txtHosts.Enabled = Not isScanning
        cboPreset.Enabled = Not isScanning
        numTimeout.Enabled = Not isScanning
        numThreads.Enabled = Not isScanning
        chkPing.Enabled = Not isScanning
        If isScanning Then
            numMin.Enabled = False
            numMax.Enabled = False
        Else
            cboPreset_SelectedIndexChanged(Nothing, EventArgs.Empty)
        End If
    End Sub

    Private Sub RunOnUi(action As MethodInvoker)
        If Me.IsHandleCreated AndAlso Not Me.IsDisposed Then
            Try
                Me.Invoke(action)
            Catch
                ' Form có thể đã đóng giữa lúc thread nền gọi lên UI, bỏ qua an toàn
            End Try
        End If
    End Sub

    Private Sub AppendLog(text As String, color As Color)
        RunOnUi(Sub()
                    rtbLog.SelectionStart = rtbLog.TextLength
                    rtbLog.SelectionLength = 0
                    rtbLog.SelectionColor = color
                    rtbLog.AppendText(text & Environment.NewLine)
                    rtbLog.ScrollToCaret()
                End Sub)
    End Sub

End Class
