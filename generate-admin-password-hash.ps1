param(
    [Parameter(Mandatory = $true)]
    [string]$Password,

    [int]$Iterations = 210000
)

$salt = New-Object byte[] 16
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($salt)
$rng.Dispose()

$deriveBytes = [System.Security.Cryptography.Rfc2898DeriveBytes]::new(
    $Password,
    $salt,
    $Iterations,
    [System.Security.Cryptography.HashAlgorithmName]::SHA256)

$hash = $deriveBytes.GetBytes(32)

'{0}${1}${2}${3}' -f 'pbkdf2-sha256', $Iterations, [Convert]::ToBase64String($salt), [Convert]::ToBase64String($hash)
