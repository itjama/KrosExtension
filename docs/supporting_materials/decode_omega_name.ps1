param(
    [Parameter(Mandatory=$true)]
    [string]$HexName
)

# Read-only decoder inferred from the supplied Omega 29.50.0 database samples.
# The final byte is retained as metadata; its meaning is not established.
$ErrorActionPreference = 'Stop'
if ($HexName -notmatch '^(?:[0-9a-fA-F]{2}){2,}$') {
    throw 'Expected an even-length hexadecimal name with at least two bytes.'
}
$bytes = [byte[]]::new($HexName.Length / 2)
for ($i = 0; $i -lt $bytes.Length; $i++) {
    $bytes[$i] = [Convert]::ToByte($HexName.Substring(2 * $i, 2), 16)
}
$plain = [byte[]]::new($bytes.Length - 1)
$previous = 4
for ($i = 0; $i -lt $plain.Length; $i++) {
    $plain[$i] = $bytes[$i] -bxor $previous
    $previous = $bytes[$i]
}
$encoding = [Text.Encoding]::GetEncoding(1250)
[pscustomobject]@{
    Name = $encoding.GetString($plain)
    TrailingByteHex = $bytes[-1].ToString('x2')
    TrailingByteVerified = $false
}
