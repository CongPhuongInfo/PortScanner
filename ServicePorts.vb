Imports System
Imports System.Collections.Generic

''' <summary>
''' Bảng tra tên dịch vụ theo số cổng (không phụ thuộc banner, chỉ dựa theo quy ước IANA/thường gặp).
''' Dùng để hiển thị gợi ý bên cạnh banner thật lấy được từ BannerGrab.
''' </summary>
Public Module ServicePorts

    Private ReadOnly serviceMap As New Dictionary(Of Integer, String) From {
        {20, "FTP-DATA"}, {21, "FTP"}, {22, "SSH"}, {23, "Telnet"}, {25, "SMTP"},
        {53, "DNS"}, {67, "DHCP-Server"}, {68, "DHCP-Client"}, {69, "TFTP"},
        {80, "HTTP"}, {110, "POP3"}, {111, "RPCbind"}, {119, "NNTP"},
        {123, "NTP"}, {135, "MS-RPC"}, {137, "NetBIOS-NS"}, {138, "NetBIOS-DGM"},
        {139, "NetBIOS-SSN"}, {143, "IMAP"}, {161, "SNMP"}, {162, "SNMP-Trap"},
        {179, "BGP"}, {194, "IRC"}, {389, "LDAP"}, {443, "HTTPS"},
        {445, "Microsoft-DS/SMB"}, {465, "SMTPS"}, {514, "Syslog"},
        {515, "LPD"}, {520, "RIP"}, {587, "SMTP-Submission"}, {631, "IPP"},
        {636, "LDAPS"}, {873, "rsync"}, {902, "VMware-Server"}, {989, "FTPS-DATA"},
        {990, "FTPS"}, {993, "IMAPS"}, {995, "POP3S"}, {1080, "SOCKS"},
        {1194, "OpenVPN"}, {1337, "WASTE"}, {1433, "MSSQL"}, {1434, "MSSQL-Monitor"},
        {1521, "Oracle-DB"}, {1723, "PPTP"}, {2049, "NFS"}, {2082, "cPanel"},
        {2083, "cPanel-SSL"}, {2181, "ZooKeeper"}, {2375, "Docker"}, {2376, "Docker-TLS"},
        {27017, "MongoDB"}, {3000, "Dev-HTTP (Node/Rails)"}, {3128, "Squid-Proxy"},
        {3306, "MySQL"}, {3389, "RDP"}, {3690, "Subversion"}, {4444, "Metasploit/Custom"},
        {5000, "Dev-HTTP (Flask/UPnP)"}, {5060, "SIP"}, {5222, "XMPP"}, {5432, "PostgreSQL"},
        {5601, "Kibana"}, {5900, "VNC"}, {5984, "CouchDB"}, {6379, "Redis"},
        {6660, "IRC"}, {6666, "IRC"}, {6667, "IRC"}, {7001, "WebLogic"},
        {8000, "Dev-HTTP"}, {8008, "HTTP-Alt"}, {8080, "HTTP-Proxy/Alt"},
        {8081, "HTTP-Alt"}, {8086, "InfluxDB"}, {8443, "HTTPS-Alt"},
        {8888, "HTTP-Alt/Jupyter"}, {9000, "SonarQube/PHP-FPM"}, {9042, "Cassandra"},
        {9090, "HTTP-Alt/Prometheus"}, {9092, "Kafka"}, {9200, "Elasticsearch"},
        {9300, "Elasticsearch-Transport"}, {11211, "Memcached"}, {25565, "Minecraft"},
        {27015, "Source-Engine-Game"}, {32400, "Plex"}
    }

    ''' <summary>Danh sách cổng thường gặp (dùng cho preset "Cổng phổ biến"), đã sắp xếp tăng dần.</summary>
    Public ReadOnly Property TopCommonPorts As Integer()
        Get
            Dim arr(serviceMap.Count - 1) As Integer
            serviceMap.Keys.CopyTo(arr, 0)
            Array.Sort(arr)
            Return arr
        End Get
    End Property

    Public Function GetServiceName(port As Integer) As String
        Dim name As String = Nothing
        If serviceMap.TryGetValue(port, name) Then
            Return name
        End If
        Return "Không xác định"
    End Function

End Module
