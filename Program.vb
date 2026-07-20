Imports System
Imports System.Windows.Forms

Module Program
    <STAThread()>
    Sub Main()
        AddHandler Application.ThreadException, AddressOf OnUiThreadException
        AddHandler AppDomain.CurrentDomain.UnhandledException, AddressOf OnUnhandledException
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException)

        Try
            Application.EnableVisualStyles()
            Application.SetCompatibleTextRenderingDefault(False)
            Application.Run(New PortScannerForm())
        Catch ex As Exception
            MessageBox.Show("Lỗi khi khởi động:" & Environment.NewLine & ex.ToString(), "Loi khoi dong", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub OnUiThreadException(sender As Object, e As Threading.ThreadExceptionEventArgs)
        MessageBox.Show("Loi UI thread:" & Environment.NewLine & e.Exception.ToString(), "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error)
    End Sub

    Private Sub OnUnhandledException(sender As Object, e As UnhandledExceptionEventArgs)
        Dim ex As Exception = TryCast(e.ExceptionObject, Exception)
        MessageBox.Show("Loi khong xu ly duoc:" & Environment.NewLine & If(ex IsNot Nothing, ex.ToString(), "Unknown"), "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error)
    End Sub
End Module
