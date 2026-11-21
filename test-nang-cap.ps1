$ErrorActionPreference = 'Continue'
$base = 'http://localhost:5025'
$pass = 0; $fail = 0
$results = @()

function T($name, $url, $expect) {
    try {
        $r = Invoke-WebRequest -Uri ($base + $url) -WebSession $script:s -UseBasicParsing -TimeoutSec 30 -ErrorAction Stop
        $code = $r.StatusCode
        $ok = $expect -contains $code
        $len = $r.RawContentLength
    } catch {
        $resp = $_.Exception.Response
        if ($resp) { $code = [int]$resp.StatusCode; $len = 0 } else { $code = 0; $len = 0 }
        $ok = $expect -contains $code
    }
    $script:tick++
    if ($ok) { $script:pass++ } else { $script:fail++ }
    $mark = if ($ok) { 'PASS' } else { 'FAIL' }
    Write-Output ("  [{0}] {1,-46} {2,3}  {3} bytes" -f $mark, $name, $code, $len)
    return @{ name = $name; url = $url; code = $code; ok = $ok; len = $len }
}
$script:tick = 0
$script:s = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$script:pass = 0; $script:fail = 0

Write-Output '================ DANG NHAP ADMIN ================'
$login = Invoke-WebRequest -Uri "$base/Account/Login" -WebSession $s -UseBasicParsing -TimeoutSec 25
$tok = [regex]::Match($login.Content, 'name="__RequestVerificationToken"[^>]*value="([^"]+)"').Groups[1].Value
$body = @{ Email='admin@example.com'; Password='Admin@123'; __RequestVerificationToken=$tok }
Invoke-WebRequest -Uri "$base/Account/Login" -Method Post -Body $body -WebSession $s -UseBasicParsing -MaximumRedirection 0 -TimeoutSec 25 -ErrorAction SilentlyContinue | Out-Null
$chk = Invoke-WebRequest -Uri "$base/quan-tri" -WebSession $s -UseBasicParsing -TimeoutSec 25
Write-Output ("  -> da dang nhap admin (trang /quan-tri = " + $chk.StatusCode + ")")

Write-Output ''
Write-Output '================ 1) TRANG ADMIN CO BAN ================'
$results += T 'Tong quan'            '/quan-tri' 200
$results += T 'Ga tau'               '/quan-tri/Stations' 200
$results += T 'Tuyen duong'          '/quan-tri/Routes' 200
$results += T 'Doan tau'             '/quan-tri/Trains' 200
$results += T 'Chuyen tau'           '/quan-tri/Trips' 200
$results += T 'Bang gia'             '/quan-tri/Fares' 200
$results += T 'Doi tuong khach'      '/quan-tri/PassengerTypes' 200
$results += T 'Ma giam gia'          '/quan-tri/Coupons' 200
$results += T 'Don hang'             '/quan-tri/Bookings' 200
$results += T 'Hoan tien'            '/quan-tri/Refunds' 200
$results += T 'Quy & khach hang'     '/quan-tri/Finance' 200
$results += T 'Thong bao da phat'    '/quan-tri/Notifications' 200
$results += T 'Nguoi dung'           '/quan-tri/Users' 200

Write-Output ''
Write-Output '================ 2) TIM KIEM MOI (vua nang cap) ================'
$results += T 'Don hang - loc holding'       '/quan-tri/Bookings?status=holding' 200
$results += T 'Don hang - tim KHONG khop'    '/quan-tri/Bookings?keyword=zzzzkhongcogi' 200
$results += T 'Don hang - tim ma don'        '/quan-tri/Bookings?keyword=VE' 200
$results += T 'Don hang - tim email'         '/quan-tri/Bookings?keyword=example.com' 200
$results += T 'Don hang - ket hop ca 2'      '/quan-tri/Bookings?status=paid&keyword=VE' 200
$results += T 'Nguoi dung - tim KHONG khop'  '/quan-tri/Users?keyword=zzzzkhongcogi' 200
$results += T 'Nguoi dung - tim theo ten'    '/quan-tri/Users?keyword=Nguyen' 200
$results += T 'Bang gia - tim KHONG khop'    '/quan-tri/Fares?keyword=zzzzkhongcogi' 200
$results += T 'Bang gia - tim Ha Noi'        '/quan-tri/Fares?keyword=Ha%20Noi' 200

Write-Output ''
Write-Output '================ 3) XUAT CSV (vua them) ================'
function Tcsv($name, $url) {
    try {
        $r = Invoke-WebRequest -Uri ($base + $url) -WebSession $script:s -UseBasicParsing -TimeoutSec 30 -ErrorAction Stop
        $bytes = $null
        if ($r.RawContentStream) { $r.RawContentStream.Position = 0; $ms = New-Object System.IO.MemoryStream; $r.RawContentStream.CopyTo($ms); $bytes = $ms.ToArray() }
        if (-not $bytes) { $bytes = $r.Content }
        $hasBom = $false
        $text = ''
        if ($bytes -is [byte[]]) {
            $hasBom = ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
            $text = [System.Text.Encoding]::UTF8.GetString($bytes)
        } else { $text = [string]$bytes }
        $lines = ($text -split "`r?`n" | Where-Object { $_.Trim() -ne '' })
        $ct = $r.Headers['Content-Type']
        Write-Output ("  [INFO] {0,-24} {1}  CT={2}" -f $name, $r.StatusCode, $ct)
        Write-Output ("         BOM={0}  dong={1}" -f $hasBom, $lines.Count)
        Write-Output ("         header: {0}" -f $lines[0])
        if ($lines.Count -gt 1) { Write-Output ("         dong 1 : {0}" -f $lines[1]) }
        $ok = ($r.StatusCode -eq 200) -and $hasBom -and ($lines.Count -ge 1)
        $script:tick++
        if ($ok) { $script:pass++ } else { $script:fail++ }
        Write-Output ("         => {0}" -f $(if ($ok) { 'PASS' } else { 'FAIL' }))
    } catch {
        Write-Output ("  [FAIL] {0}: {1}" -f $name, $_.Exception.Message)
        $script:tick++; $script:fail++
    }
}
Tcsv 'CSV don hang' '/quan-tri/ExportBookings'
Tcsv 'CSV don hang + loc' '/quan-tri/ExportBookings?status=paid'
Tcsv 'CSV nguoi dung' '/quan-tri/ExportUsers'
Tcsv 'CSV bang gia' '/quan-tri/ExportFares'

Write-Output ''
Write-Output '================ 4) TRANG KHACH KHONG BI VO ================'
$anon = New-Object Microsoft.PowerShell.Commands.WebRequestSession
function Ta($name, $url, $expect) {
    try {
        $r = Invoke-WebRequest -Uri ($base + $url) -WebSession $anon -UseBasicParsing -TimeoutSec 30 -ErrorAction Stop
        $code = $r.StatusCode
    } catch {
        $resp = $_.Exception.Response
        if ($resp) { $code = [int]$resp.StatusCode } else { $code = 0 }
    }
    $ok = $expect -contains $code
    $script:tick++
    if ($ok) { $script:pass++ } else { $script:fail++ }
    Write-Output ("  [{0}] {1,-46} {2}" -f $(if ($ok) { 'PASS' } else { 'FAIL' }), $name, $code)
}
Ta 'Trang chu'              '/' 200
Ta 'Tim chuyen'             '/Home/Search?fromId=1&toId=4&date=2026-12-01' 200
Ta 'Dang nhap'              '/Account/Login' 200
Ta 'Dang ky'                '/Account/Register' 200
# Admin khi chua dang nhap: phai 302 -> /Account/Login (dung HttpWebRequest de doc header that)
$code0 = 0; $loc0 = ''
$req0 = [System.Net.HttpWebRequest]::Create("$base/quan-tri/Bookings")
$req0.AllowAutoRedirect = $false
$req0.CookieContainer = New-Object System.Net.CookieContainer
$req0.Timeout = 20000
try {
    $resp0 = $req0.GetResponse()
    $code0 = [int]$resp0.StatusCode; $loc0 = [string]$resp0.Headers['Location']
    $resp0.Close()
} catch [System.Net.WebException] {
    $resp0 = $_.Exception.Response
    if ($resp0) { $code0 = [int]$resp0.StatusCode; $loc0 = [string]$resp0.Headers['Location']; $resp0.Close() }
}
$ok0 = ($code0 -eq 302) -and $loc0.Contains('/Account/Login')
$script:tick++
if ($ok0) { $script:pass++ } else { $script:fail++ }
Write-Output ("  [{0}] {1,-40} {2} -> {3}" -f $(if ($ok0) { 'PASS' } else { 'FAIL' }), 'Admin chua dang nhap', $code0, $loc0)

Write-Output ''
Write-Output '================================================'
Write-Output ("  TONG: {0} phep thu | PASS {1} | FAIL {2}" -f $script:tick, $script:pass, $script:fail)
Write-Output '================================================'
