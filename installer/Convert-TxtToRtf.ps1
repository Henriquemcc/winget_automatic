param (
    [Parameter(Mandatory=$true)]
    [string]$SourcePath,

    [Parameter(Mandatory=$true)]
    [string]$DestinationPath
)

try {
    # Checks if the source file exists.
    if (-not (Test-Path -Path $SourcePath)) {
        Write-Error "Error: The source file was not found in '$SourcePath'."
        return
    }

    # Loads the assembly required for manipulating formatted text.
    Add-Type -AssemblyName System.Windows.Forms

    # Creates a silent instance of RichTextBox.
    $richTextBox = New-Object System.Windows.Forms.RichTextBox

    # Read the contents of the text file.
    $content = Get-Content -Path $SourcePath -Raw -Encoding UTF8

    # Assigns the content to the control.
    $richTextBox.Text = $content

    # Adding font
    $richTextBox.SelectionFont = $richTextBox.Font = [System.Drawing.Font]::new("Calibri", 12, [System.Drawing.FontStyle]::Regular)

    # Ensures that the destination directory exists.
    $destDir = Split-Path -Parent $DestinationPath
    if ($destDir -and -not (Test-Path $destDir)) {
        New-Item -ItemType Directory -Path $destDir | Out-Null
    }

    # Saved as RTF
    $richTextBox.SaveFile($DestinationPath, [System.Windows.Forms.RichTextBoxStreamType]::RichText)

    Write-Host "Success: File converted and saved to: $DestinationPath" -ForegroundColor Green
}
catch {
    Write-Error "An error occurred during the conversion: $($_.Exception.Message)"
}
finally {
    # Release the object's resources
    if ($null -ne $richTextBox) {
        $richTextBox.Dispose()
    }
}